![image](https://github.com/user-attachments/assets/e1840799-4e10-4ed6-ab33-47d6c27b96f2)

Malcrow is a application that creates fake processes and registry keys. It does this in an attempt to prevent certain types of malware from running on your computer.
In a sense it mocks an analysis environment which most malware attempts to avoid running in to prevent any reversing of the malware itself. This is why it's concidered a malware scarecrow.

The idea was put together after testing around with another malware scarecrow that I saw. This was the semi famous Cyber Scarecrow (https://www.cyberscarecrow.com/).
I like the idea of the software but I didn't like the idea that they weren't open source for a project like this.

The goal of this program is to be a better version of Cyber Scarecrow and to be open source.


## What does Malcrow do currently?

Malcrow does the following things currently:
- Auto creates, shuffles the hash, and runs the fake processes based on what settings you have set (Hash shuffling is to prevent detections from malware)
- Uses very little CPU/RAM power with the fake processes
- Monitors the background processes and provides CPU/RAM usage on main screen
- All fake processes close automatically if the main processes gets terminated
- All fake processes auto delete after closing (if not terminated)
- Creates registry keys and stores them in a local file so they can be deleted on next launch if process is terminated


## Will Malcrow keep me safe?

This is subjective but in a sense yes.. but only from certain malware families. It's a good backup software to have running to help prevent malware from running but by no means does it replace a anti-virus.
Nor should you look at Malcrow as an anti-virus.

## Will Malcrow affect other software?

Yes, there is a high potential to affect other software depending on what settings you have. Settings such as decompiler/dumping tools could cause software to not run.
This is easy to remedy though, simply stop Malcrow, untoggle certain software, and restart Malcrow.
