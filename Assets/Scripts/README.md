

* [Watch the video here](https://imgur.com/a/FQ5joo3).
* Simulates Heat and Gas flow across a 2d grid that can have obstacles and variable Temperature, Conductivity, and Thermal Capacity
* It all runs on the Graphics card and thus can handle a one million grid cells at over 300 simulation ticks per second on any decent GPU. The simulation itself is far from perfect, but decent for simple situations, and fast.
* On the left is the net gas movement per tile, the middle is the pressure per tile, and the right is a 1d cross section of the bottom row of tiles so you can get a better idea for how shockwaves dissipate and bounce off walls. The whole thing is contained in a box, and there is a small wall (black line) at the bottom which you can see in the middle box. 
