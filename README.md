# AlwaysTooLate.Commands

AlwaysTooLate Commands module, a simple, universal and easy to use commands solution, that can be used for developer commands, cheats, cvars, etc.

# Installation

Before installing this module, be sure to have installed these:

- [AlwaysTooLate.Core](https://github.com/AlwaysTooLate/AlwaysTooLate.Core)

Open your target project in Unity and use the Unity Package Manager (`Window` -> `Package Manager` -> `+` -> `Add package from git URL`) and paste the following URL:
https://github.com/AlwaysTooLate/AlwaysTooLate.Commands.git

# Setup

After succesfull installation, open a scene that is loaded first when starting your game (we recommend having an entry scene called Main that is only used for initializing core systems and utilities, which then loads the next scene, that is supposed to start the game - like a Main Menu). In that scene, create an empty GameObject and attach the CommandsManager component to it.

# Basic Usage

To register your own command, use the `CommandManager.Instance.RegisterCommand` method (it is recommended to register all commands in the Main scene, by using the `Awake` method on some component in that scene).

# Contribution

We do accept PR. Feel free to contribute. If you want to find us, look for the ATL staff on the official [AlwaysTooLate Discord Server](https://discord.alwaystoolate.com/)

*AlwaysTooLate.Commands (c) 2018-2020 Always Too Late.*
