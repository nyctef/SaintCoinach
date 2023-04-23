# parse json from SaintCoinach/Definitions/Quest.json

import json

with open("SaintCoinach/Definitions/Quest.json") as f:
    data = json.load(f)

definitions = data["definitions"]

currentIndex = 0

# for each definition in definitions, check the index and see if it's sequential from the previous index

for definition in definitions:
    index = definition.get("index", 0)
    name = definition.get("name", "<unnamed>")
    if name is "<unnamed>" and definition.get("definition") is not None:
        name = definition["definition"].get("name", "<unnamed>")

    incr = 1
    if definition.get("type") == "repeat":
        incr = int(definition["count"])

    if (
        definition.get("definition") is not None
        and definition["definition"].get("type") == "repeat"
    ):
        incr *= int(definition["definition"]["count"])

    if index != currentIndex:
        print(
            f"gap found around {name}: expecting to see index {currentIndex} next,"
            f" but got {index} instead ({index-currentIndex})"
        )
        currentIndex = index + 1
    else:
        currentIndex += incr
