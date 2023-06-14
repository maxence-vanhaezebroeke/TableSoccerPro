# TableSoccerPro

I made a multiplayer game based on the real table soccer game, using Unity. It is possible to play on LAN & Online, from 2 to 4 players.

Players can control bars, from which they can switch control using A/Z/E/R keys. (planning to support QWERTY keyboards later)  
A 2-player game means that every player is controlling 4 bars, each player on each side of the field.  
A 4-player game means that every player is controlling 2 bars, either from the attack (attackers and 5-player bar) or from the defense (goalkeeper and defenders) from each side of the field.

Players can move the bars up & down using mouse Y, and rotate the bars using mouse scroll.  
Players can also do a power-shot by using the left click : a powerful shots that rotates the bar at full speed towards the other team's net, and send back the bar as its initial rotation.

*Insert in-game GIFs here!*

# Table of contents
* [About development](#about-development)
* [Launching the game](#launching-the-game)
* [Start host / Start client](#start-host-or-client)
* [LAN](#lan)
* [Online](#online)
* [Client-side prediction](#client-side-prediction)

## About development

I used Unity 2020.3.20. I've created the models, scripts, shaders, UI, cinematic animation (using C# scripts), particles - except the soccer ball that I used from a free Asset inside the Unity Asset Store, and textures.

I started using [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/) to implement the multiplayer.  
Everything was working great, except that in order to follow the server authoritative architecture, I had to send inputs from clients to server, and send back the up to date data - which
caused a lot of delay. (I'm using physics for both the ball and the bars, which can have different owners)  
I didn't want to make the game client authoritative, and instead focus on a solution that most multiplayer fast-paced games would implement : **client-side prediction**.

That being said, NGO is **not** at this date *(May 2023)* giving tools to implement client-side predictions. So, I decided to switch the networking library to [Fish-Net](https://fish-networking.gitbook.io/docs/).

## Launching the game

I made a cinematic before getting to the main menu. Once landed on the main menu, you can choose between playing in LAN or playing Online, and choose between playing with 2 or 4 players.

*I would really like to insert GIF here too...*

## Start host or client

Inside the game, once you chose between LAN & Online, you can start the game either as :
* A host
* A client

As a host, a server instance is running in the background, while the client instance is starting, launching the player inside the game, waiting for others.
For LAN players, I searched ways to remove searching servers by IP addresses, as it is just not convenient.
As for Online players, I made the join code visible for host players while in-game, so that they can share it with clients.

## LAN

I used Fish-Networking-Discovery (an Add-on of Fish-Net) to enable network discovery, which removed the needs to put the IP address of the server inside the client instance.
As a client, a client instance is running and starts searching for servers available inside the same network.

## Online

I used Unity Relay services to create a multiplayer game, so that clients can join using a join code.
I added an input text field inside the Online sub-menu, so that client can write the join code, given by the host. Host players can see the
join code on top of their screen, red font, and can hide it using F2 key.  
This is for me easy to use, and fits the job as playing with friends online. Unfortunatly, free Unity Relay services are designed for
high-pace games, so the high latency is an important drawback to my game.

## Client-side prediction

Soccer bars are predicted network objects : controlled by the player, they can interact with the ball, which is owned by the server.  
I used the first version of client-side prediction from Fish-Net, and I plan to use the second version later.  
I tried making the powershot works with client-side prediction : since its a multiple of physics operations under half a second, it can
be tricky to synchronise it over the network. I still have some synchronizing issues to solve (bars jittering), but other than that, 
clients can move and interact with the ball as expected.
