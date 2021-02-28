# The Lone Cabin source samples
The Lone Cabin is a top-down action game made with [Unity](https://unity.com/) where you play as a hiker who finds... a lone cabin in the woods. 

This repository contains a small selection of the game's source code. You can download the full playable from the [latest release](https://github.com/pekkahe/the-lone-cabin-samples/releases/latest).

For more details about the game, please visit [pekkahellsten.com](https://pekkahellsten.com/).

## Sample directories

#### `\ai-behaviours`
Most of the game's AI code, which is divided into three different enemy behaviours, their base class and the controller operating them. 
Each enemy GameObject has its own controller and all the behaviours. 

The controller triggers the behaviours based on events, such as whether the player has been seen (by raycasting) or heard (simulated by firing overlap spheres).

#### `\checkpoint-system`
The system responsible for restoring the game objects' state to a previous state when the player dies. 

When a checkpoint is triggered the system creates copies of specific tagged GameObjects, moves them into a GameObject container, and disables them. 
When the player dies, the ingame objects are deleted and the copies are restored. 
		
#### `\misc`
Miscellaneous individual scripts attached to the player, enemies, or environment.	
	
#### `\path-finding`
Path finding related code for enemies. Path finding is implemented using the A* algorithm, which tries to find the shortest path between two points using a node visibility graph.

In the game, nodes are represented as GameObjects that are placed in the scene (see `Waypoint.cs`). Each enemy GameObject has a PathFinder component that provides them the interface for path finding.
