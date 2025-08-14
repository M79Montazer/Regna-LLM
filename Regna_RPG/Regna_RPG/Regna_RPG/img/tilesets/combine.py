from PIL import Image
import os
import glob
import re
from collections import defaultdict

# Define your suffix groups
group_suffixes = ["_A1", "_A2", "_A3", "_A4", "_A5", "_B", "_C", "_D", "_E"]

# Get the script's directory
script_dir = os.path.dirname(os.path.abspath(__file__))

# Get all .png files in the directory
all_png_files = glob.glob(os.path.join(script_dir, "*.png"))

# Prepare a dictionary to group images
grouped_images = defaultdict(list)

# Sort and group files by suffix
for file_path in sorted(all_png_files):
    filename = os.path.basename(file_path)
    for suffix in group_suffixes:
        if suffix in filename:
            grouped_images[suffix].append(file_path)
            break  # Avoid assigning the same file to multiple groups

# Create and save combined images for each group
for suffix, files in grouped_images.items():
    if not files:
        continue

    # Load images
    images = [Image.open(f).convert("RGBA") for f in files]

    # Calculate canvas size
    max_width = max(img.width for img in images)
    total_height = sum(img.height for img in images)

    # Create a new transparent image
    combined = Image.new("RGBA", (max_width, total_height), (0, 0, 0, 0))

    # Paste all images vertically
    y_offset = 0
    for img in images:
        combined.paste(img, (0, y_offset), mask=img)
        y_offset += img.height

    # Save the combined image
    output_filename = f"combined{suffix}.png"
    output_path = os.path.join(script_dir, output_filename)
    combined.save(output_path)
    print(f"Saved: {output_path}")
