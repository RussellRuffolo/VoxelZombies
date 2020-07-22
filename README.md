# Voxel Zombies

This is a mutliplayer Unity game based around a custom voxel engine. It runs on Unity version 2019.2.21f1

# Gameplay

Players spawn in a 3D voxel environment as humans. After 30 seconds a player is chosen to be a zombie. Zombies may infect humans by colliding with them. If all humans become infected the zombies win. If a round ends with humans alive those humans win. After the round time is up a new map is loaded and the process repeats.

# Current Features

### Authoratative Server with Client Prediction

A central server handles all client input and is the ground truth for the game. Each client predict their state based on their input in order to prevent latency. When a client receives a state message from the server that contradicts their prediction, the client uses the server state as a new base and simulate forward to their current tick. 

Here is a comparison between the client prediction with the inputs and how the server handles those same inputs. First is the client code, which records user inputs, runs them, and sends them to the server. 
![ClientSimulation](/Screenshots/ClientSimulation.png)

Now the server code, which runs the client inputs and sends the resulting states back to the client. Notice how the ApplyInputs() function behaves identically in both cases.

![ServerSimulation](/Screenshots/ServerSimulation.png) 

In order to combat packet loss, the client stores its inputs and sends every unconfirmed input to the server each frame in a UDP message. Whenever the server receives an input message it applies all unapplied inputs and confirms to the client that it has executed those inputs so the client can stop sending them. The server also tells the client the resulting state from those inputs. The client then compares the stored state at that tick to the true state from the server. If they are different the client goes back to the true state and simulates forward to the current client tick. 

Here is a look at the client rollback code

![ClientRollback](/Screenshots/ClientRollback.png)

This system ensures instant responsiveness from the client in cases of latency and packet loss while still giving the server complete authority over the game state. 

### Voxel Based World

The world of Voxel Zombies is represented by a list of block IDs that are used to populate a 3D grid with each block's vertices. Rendering of these vertices is split into 16x16x16 block chunks,  for quick rerendering when blocks are changed. This allows for real time building and breaking by players. Different blocks are usually distinquished by texture, but half height and liquid blocks are also supported. 

### Player Movement

In a world where one misstep can mean death by zombie, fluid and responsive movement is very important. Voxel Zombies uses a custom rigidbody-based character controller. Movement was designed with the voxel maps in mind, so as to create smooth parkour around 1x1x1 meter blocks. Movement is affected by various states, such as being in the air, being in the water, and climbing half-block stairs.

### Chat System

The chat system on the client side has the ability to send, receive, and display chat messages. The server side implements chat commands such as map voting and queries such as round time left. 


# ToDo
* Add lava physics
* Add decorative blocks
* Replace placeholder textures

# Current Screenshots

Client view of the map "Diametric" featuring translucent water

![Diametric](/Screenshots/DiametricClient.png)

Client view of the map "Carson" that shows the block selection square. 

![Carson](/Screenshots/CarsonClient.png)

# Old Screenshots

Mainstreet of the map "Carson" viewed from the server

![Carson](/Screenshots/CarsonServer.png)


The map "Pandora's Box" viewed from the client

![Pandora](/Screenshots/PandoraClient.png)

An overview of the map "8Bit" viewed from the server

![8Bit](/Screenshots/8BitServer.png)

A closeup of the map "Asylum" viewed from the server

![Asylum](/Screenshots/AsylumServer.png)


