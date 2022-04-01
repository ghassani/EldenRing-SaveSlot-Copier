# Elden Ring Save Slot Copier

This command line tool will copy an Elden Ring save slot from one to another either in place or to a separate output file. This tool requires that a game save already exists in the target slot otherwise you will not be able to load the slot after copy. It also assumes that game saves are stored sequentially, and has not been tested with any save slots that have been deleted in between slots.

Requires .net 6.0

Test on version 1.03.02

## Usage

1. Open Elden Ring
2. Create a new game in desired slot to copy to. Save and exit as soon as you control the character.
3. Close Elden Ring (wait for it to fully close)
4. Open your Elden Ring save file directory. `%AppData%\EldenRing` and make a backup of your existing save to a safe location.
5. Copy the file `ER0000.sl2` from your steamid directory inside the EldenRing save path above to the location where you extracted/built this program
6. Open a command prompt or power shell instance and navigate to where you extracted this program
7. Run the following command (replacing required information in the file path): `EldenRing-SaveSlotCopier.exe --input ER0000.sl2 --source 1 --dest 2` - This will copy the save in slot 1 to the save in slot 2. Adjust the source and dest parameters to your needs.
8. Copy the modified save file back into your elden ring save folder. Copy again over the .bak version