/*:
 * @target MZ
 * @plugindesc v1.5 LLM Chat → face+text → getPlayerName
 *
 * @command chatFace
 * @text Chat with Face
 * @desc Prompt player → call API → show face+text in one go.
 *
 * @arg faceName
 * @type string
 * @text Image File
 * @desc The filename under img/faces (no extension), e.g. “Actor1”.
 * @default Actor1
 *
 * @arg faceIndex
 * @type number
 * @min 0
 * @max 7
 * @text  Face Index
 * @desc  Which cell (0-7) in that face sheet.
 * @default 0
 *
 * @arg promptLabel
 * @type string
 * @text  Prompt Label
 * @desc  What to show in the window.prompt() title (e.g. “You say:”).
 * @default You say:
 *
 * @arg npcId
 * @type string
 * @text  name of the npc
 * @desc  who is the player talking to?
 * @default quest_giver
 *
 * @arg eventId
 * @type string
 * @text id of the event in the map
 * @desc what event number is this event
 * @default 0
 *
 * @command getPlayerName
 * @text Get Player Name
 * @desc get a name from player and set it in var=21
 *
 *
 *
 */
const IS_PRODUCTION = false;
function getBackendPort() {
    try {
        if (typeof require === 'function') {
            const fs = require('fs');
            const path = require('path');
            const base = (typeof process !== 'undefined' && process.cwd) ? process.cwd() : '.';
            const p = path.join(base, 'backend_port.txt');
            if (fs.existsSync(p)) {
                const txt = fs.readFileSync(p, 'utf8').trim();
                if (txt.length > 0) return txt;
            }
        }
    } catch (e) {
        // ignore
    }
    return '5000'; // fallback
}

function getPlayerName() {
    if (!IS_PRODUCTION) {
        return "MM";
    }
    const promptText = "Please enter your name:";
    const defaultName = "Name";

    // read variable
    let name = $gameVariables.value(21);
    if (typeof name === "string") name = name.trim();

    // if already set and non-empty, return it
    if (name && name.length > 0) return name;

    // otherwise prompt synchronously (works in NW.js)
    const raw = prompt(promptText, defaultName);
    const final = (raw === null || String(raw).trim() === "") ? defaultName : String(raw).trim();

    // store into game variable for future use
    $gameVariables.setValue(21, final);

    return final;
}

function wrapTextByLength(text, maxLineLength = 70) {
    const lines = [];
    const paragraphs = text.split(/\r?\n/); // Split at \n or \r\n

    for (let paragraph of paragraphs) {
        paragraph = paragraph.trim();
        while (paragraph.length > maxLineLength) {
            // Find the last space within the limit
            let breakIndex = paragraph.lastIndexOf(" ", maxLineLength);

            if (breakIndex === -1) {
                // No space found: force break mid-word
                breakIndex = maxLineLength;
            }

            const line = paragraph.slice(0, breakIndex).trim();
            lines.push(line);
            paragraph = paragraph.slice(breakIndex).trim();
        }

        // Push the remainder of the paragraph if it exists
        if (paragraph.length > 0) {
            lines.push(paragraph);
        }

        // After each paragraph, add a blank line (to preserve \n)
        lines.push("");
    }

    // Remove extra empty line at the end if any
    if (lines[lines.length - 1] === "") {
        lines.pop();
    }

    return lines.join("\n");
}
function simulateOK() {
    setTimeout(() => {
        Input._currentState["ok"] = true;
        setTimeout(() => {
            Input._currentState["ok"] = false;
        }, 100);
    }, 100);
}

function grantBlessing() {
    $gameActors.actor(1).gainExp(2000);

    AudioManager.playSe({ name: "Heal1", volume: 90, pitch: 100, pan: 0 });
    $gameScreen.startFlash([255, 255, 255, 160], 60);
}

function setPlayerHasKey() {
    // Set a game variable (e.g., variable 10 = has key)
    $gameSwitches.setValue(45, true);

    // Optionally, show visual feedback
    AudioManager.playSe({ name: "Chest1", volume: 90, pitch: 100, pan: 0 });
    $gameScreen.startFlash([255, 255, 128, 160], 30);
}
function endConversation(eventId) {
    // Self Switches use keys like [mapId, eventId, 'D']
    const mapId = $gameMap.mapId();
    $gameSelfSwitches.setValue([mapId, eventId, 'D'], true);

}

function setNpcAttack() {
    $gameSwitches.setValue(43, true);
    AudioManager.playSe({ name: "Sword1", volume: 90, pitch: 100, pan: 0 });
}


(() => {
    const PLUGIN = "SimpleLLM";

    Input.keyMapper[13] = "ok";
    Input.keyMapper[32] = "";
    Input.keyMapper[87] = "up";
    Input.keyMapper[88] = "";
    Input.keyMapper[90] = "";
    Input.keyMapper[68] = "right";
    Input.keyMapper[65] = "left";
    Input.keyMapper[83] = "down";

    PluginManager.registerCommand(PLUGIN, "chatFace", args => {
        // 1) ask
        const playerInput = window.prompt(args.promptLabel || "You say:");

        //const playerInput = $gameVariables.value(69);
        if (!playerInput) return;

        $gameMessage.clear();
        $gameMessage.add("…thinking…");
        SceneManager._scene._messageWindow.startMessage();

        let port;
        let url;

        if (IS_PRODUCTION) {
            port = getBackendPort();
            url = new URL("http://localhost:" + port + "/Test/Generate");
        } else {
            url = new URL("https://localhost:7072/Test/Generate");
        }
        url.searchParams.set("prompt", playerInput);
        url.searchParams.set("npcId", args.npcId || "");
        url.searchParams.set("playerName", getPlayerName());
        fetch(url)
            .then(res => res.json())
            .then(json => {
                /*// store in variable?
                const vid = Number(args.varId) || 0;
                if (vid > 0) {
                  $gameVariables.setValue(vid, reply);
                }*/
                let reply = json.response;
                const action = json.action || "";
                $gameMessage.clear();
                $gameMessage.setFaceImage(args.faceName, Number(args.faceIndex));
                reply = wrapTextByLength(reply);
                $gameMessage.add(reply);
                SceneManager._scene._messageWindow.startMessage();
                simulateOK();


                const giveItemRegex = /^(?:Give[_]?Item|GiveItem)\s*\(\s*(?:(?:item[_]?name|itemName)\s*=?\s*)?['"]?([A-Za-z0-9_]+)['"]?\s*\)/i;
                const match = action.match(giveItemRegex);
                if (match) {
                    const itemName = match[1];
                    if (itemName.toLowerCase() === "blessing") {
                        grantBlessing();
                    } else if (itemName.toLowerCase() === "key") {
                        setPlayerHasKey();
                    }
                }

                const endConvRegex = /^(?:End[_]?Conversation)/i;
                if (endConvRegex.test(action)) {
                    endConversation(args.eventId);
                }

                const attackRegex = /^(?:Attack)/i;
                if (attackRegex.test(action)) {
                    setNpcAttack(args.eventId);
                }

            })
            .catch(err => {
                console.error(err);
                $gameMessage.clear();
                $gameMessage.add("…sorry, something went wrong.\n" + err);
                SceneManager._scene._messageWindow.startMessage();
                simulateOK();
            });
    });




    PluginManager.registerCommand(PLUGIN, "getPlayerName", args => {
        if (IS_PRODUCTION) {
            $gameSwitches.setValue(60, true);
        }
        getPlayerName();
    });

})();


