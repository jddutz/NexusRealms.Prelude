import os, json

# Load data from Color_Hex_RGB_HSL.json
if __name__ == "__main__":
    with open("Color_Hex_RGB_HSL.json") as f:
        data = json.load(f)
    json.dump(data, open("Color_Hex_RGB_HSL.json", "w"), indent=None)
