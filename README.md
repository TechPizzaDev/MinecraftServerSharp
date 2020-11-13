# ![](icon.png) MCServerSharp
Minecraft server implementation in C# and NET5, not affiliated with Mojang.  

This is currently just a passion project.

<br>

# Overview

#### 🧩🌎 Extendable and Cross-platform
Component-based plugins will play a big role in implementing game logic and extensibility.
Utilizing a modern and unified .NET allows the server to run on most mainstream devices and architectures. 

#### ⚡🧵 Performant and Threaded 
This implementation aims to provide great performance by utilizing modern practices, new technologies, and few allocations. 
Threading many aspects of the server is of great importance as modern/server processors usually have plenty of cores. 
One thread per dimension is planned. 

#### 💡⚒️ Innovative and Expansive
The purpose of this project is to innovate where the vanilla server is lacking, creating heavily optimized components for various workloads. A robust world save format with backups and distributed hosting is planned. 

<br>

# Features
The server will be a shell for plugins.
This includes systems like chunks, blocks, entities, items, and many more.  
Vanilla will then be implemented as a plugin.

Roadmap to be announced

<br>

# Source
1. Clone the source: `git clone https://github.com/TechnologicalPizza/MinecraftServerSharp`  
    - Feel free to fork the project or contribute
1. Set up submodules: `git submodule update --init`  
1. Open the solution (latest Visual Studio 2019 with '.NET desktop development' should "just work")  

<br>

# Scrapped
- Running a JVM for existing Bukkit/Spigot plugins or even mods, albeit not completely out of the question. It may be possible to provide an abstraction for a Java API but it would probably result in duplication of many objects at runtime.