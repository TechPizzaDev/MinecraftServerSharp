# ![](icon.png) MCServerSharp
Minecraft server implementation in C#  
Note that this is currently just a passion project and the points below are more of a vision for the future. 

***

#### 🧩🌎 Extendable and Cross-platform
Component-based plugins will play a big role in implementing game logic and later on extensibility similar to mainstream Minecraft servers. 
Utilizing a modern and unified .NET allows the server to run on most mainstream devices and architectures. 

#### 💡 Innovative
The purpose of this project is to innovate where the vanilla server is lacking, creating components optimized for various workloads. A more robust world save format with easy backups is planned. 

#### ⚡🧵 Performant and Threaded 
This implementation aims to provide great performance by utilizing modern practices, new technologies, and few allocations. 
Threading many aspects of the server is of great importance as modern/server processors usually have plenty of cores. 

#### ⚒️ Expansive
Running a JVM for Bukkit/Spigot plugins or Fabric mods is not completely out of the question if development reaches a reasonable level of maturity. The Forge API would require significant effort though. Consuming an abstract Minecraft-like API for this will certainly require certain features like threading to be disabled. 

***
