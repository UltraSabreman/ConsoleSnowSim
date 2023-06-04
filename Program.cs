using System;
using System.Collections.Generic;
using System.Text;

//TODO: Make snow piles multi-layer
//TODO: Make some static variables to eliminate the "magic numbers" throughout the code. 
//TODO: Cleanup Code
//TODO: More efficent drawing?

namespace ConsoleSnowSim {
    class Snowflake {
        public static readonly string FlakeModels = ".,*+%#";
        private static readonly Random rng = new();
        private static readonly short maxLayers = 20;

        private double layer = 1;

        private double xPos = 0;
        private double yPos = 0;

        private double oldXPos = 0;
        private double oldYPos = 0;

        private double depthAtFlake = 0;
        private bool updateOldPos = true;

        /// <summary>
        /// Fired when the flake reaches the bottom of the screen.
        /// </summary>
        public event EventHandler ReachedBottom;

        /// <summary>
        /// Fluffiniess of the snowflake, based on the avaliable characters. 
        /// </summary>
        public int Weight { set; get; } = 1;

        //These are used by the console so i don't have to keep rounding values in code. This also keeps flakes on screen.
        public int RoundXPos => (int)Math.Round(xPos) % (Console.WindowWidth - 2);
        public int RoundYPos => (int)Math.Round(yPos) % (Console.WindowHeight - 1);

        public double XPos {
            set => xPos = value < 0 ? Console.WindowWidth - 1 + value : value;
            get => xPos;
        }
        public double YPos {
            set {
                //if We reached the bottom, reset to the top.
                if ((int)Math.Round(value) >= (Console.WindowHeight - depthAtFlake)) {
                    layer = (short)(rng.Next(10, 255) % maxLayers); //Reshuffle layer
                    ReachedBottom?.Invoke(this, EventArgs.Empty); //Trigger Bottom Event.
                    yPos = 0;
                } else
                    yPos = value;
            }
            get => yPos;
        }

        /// <summary>
        /// Initiate a snowflake with random values unless parameters are passed
        /// </summary>
        /// <param name="x">X Position</param>
        /// <param name="y">Y Position</param>
        /// <param name="l">Layer, 1-3</param>
        /// <param name="w">Weight, 0-4</param>
        public Snowflake(int x = -1, int y = -1, short l = -1, int w = -1) {
            XPos = x == -1 ? rng.Next(0, Console.WindowWidth - 1) : x;
            YPos = y == -1 ? rng.Next(0, Console.WindowHeight) : y;
            layer = l == -1 ? (short)(rng.Next(10, 255) % maxLayers) : l;
            Weight = w == -1 ? rng.Next(0, FlakeModels.Length) : w;
        }

        /// <summary>
        /// Clear the last snowflake position
        /// </summary>
        private void Erase() {
            Console.CursorLeft = (int)oldXPos;
            Console.CursorTop = (int)oldYPos;
            Console.Write(" ");

            updateOldPos = true;
        }

        /// <summary>
        /// Draw current snowflake position
        /// </summary>
        public void Display() {
            Erase();

            Console.CursorLeft = RoundXPos;
            Console.CursorTop = RoundYPos;

            var test = (layer+6) * 10;

            Console.Write("\x1b[38;2;" + test + ";" + test + ";" + test + "m" + FlakeModels[Weight]);
        }

        /// <summary>
        /// Update snowflake position
        /// </summary>
        /// <param name="WindForce">Amount of wind to apply to x position</param>
        /// <param name="depth">The depth of the snow pile at in the current column</param>
        public void Tick(double WindForce = 0, double depth = 0) {
            depthAtFlake = depth;
            //Update old position before over-writing it so we can cleanly erase old snowflake
            if (updateOldPos || oldXPos >= Console.WindowWidth - 1) {
                oldXPos = RoundXPos;
                oldYPos = RoundYPos;
                updateOldPos = false;
            }

            double tickDescent = 1;

            //Modify descent rate between 1 and 0.5 (0.5 for rear-most layer).
            //Topmost layer will stay at almost 1, backmost at 0.5
            if (layer != maxLayers) {
                double layerSlowdown = 0.5 - ((layer / maxLayers) * 0.5);

                tickDescent -= layerSlowdown;
            }

            //small slowdown for "fluffiness" 
            tickDescent -= 0.25 / (Snowflake.FlakeModels.Length - Weight);

            YPos += tickDescent;

            //if we have wind, apply it. Less force for background layers, more force for fluffy snowflakes
            if (WindForce != 0)
                XPos += Math.Round(10 * (layer / maxLayers / 10)) + (0.5 / (Weight + 1));

            //Randomly shift the flake around one tile left or right to make it look more natural
            if (rng.Next(0, 50) == 0)
               XPos += rng.Next(0, 2) == 0 ? -1 : 1;
        }
    }

    internal class SnowPile {
        private List<double> piles = new();
        private List<double> oldPiles;
        private readonly int pileLayer;

        /// <summary>
        /// Creates a screen-sized pile with 0 snow
        /// </summary>
        public SnowPile(int Layer=5) {
            pileLayer = Layer;
            for (int i = 0; i < Console.WindowWidth - 1; i++)
                piles.Add(0);

            oldPiles = new List<double>(piles);
        }

        /// <summary>
        /// Gets the snow depth at a flakes position. Used to figure out when to stop drawing flakes.
        /// </summary>
        /// <param name="flake">The flake to check</param>
        /// <returns>Snow depth</returns>
        public double DepthAtFlake(Snowflake flake) => piles[flake.RoundXPos] + 1;

        /// <summary>
        /// "Adds" a snowflake to the pile.
        /// </summary>
        /// <param name="toAdd">Snowflake to add</param>
        public void AddFlake(Snowflake toAdd) {
            int pos = toAdd.RoundXPos;

            //The amount each flake fills up a slot is based on it's characters and the total amount of possible characters  

            //Devision by 4 is temporary untill multi-layered piles are in.
            piles[pos] += ((double)toAdd.Weight / (double)Snowflake.FlakeModels.Length) /4;

            //Roll-off code. We don't want any one pile to build like a tower, so we distribute it if it gets too tall.
            //This code also wraps around the edges of the screen like everything else.
            int lPos = pos != 0 ? pos - 1 : piles.Count - 1;
            int rPos = pos != Console.WindowWidth - 2 ? pos + 1 : 0;

            while (piles[pos] - piles[lPos] > 2) {
                piles[lPos]++;
                piles[pos]--;
            }

            while (piles[pos] - piles[rPos] > 2) {
                piles[rPos]++;
                piles[pos]--;
            }
        }

        /// <summary>
        /// Melts one layer from one pile randomly. Seems to keep up fairly well with a standard terminal size
        /// </summary>
        public void Melt() {
            int ind = new Random().Next(0, piles.Count);

            if (piles[ind] > 1)
                piles[ind]--;
        }

        /// <summary>
        /// Displays the snow layer.
        /// </summary>
        public void Display() {
            int xPos = 0;
            Console.ForegroundColor = ConsoleColor.White;

            foreach (double pile in piles) {
                //Fully erase the pile if it changed, ie was rolled-over
                if (pile < oldPiles[xPos])
                    DrawPile(xPos, true);

                //(Re)draw the pile of fully filled snow blocks (^1 == last array element)
                int y = DrawPile(xPos);


                DrawSmoothingLayer(y, xPos);

                xPos++;
            }
            //Update the old pile list for comparisons. 
            oldPiles = new List<double>(piles);
        }

        /// <summary>
        /// Draws a snow pile at the specified pos. If Erase is set, will go one more up to over-ride the "smooth" layer.
        /// </summary>
        /// <param name="xPos">X Position of the pile</param>
        /// <param name="Erase">Should the pile be erased?</param>
        private int DrawPile(int xPos, bool Erase = false) {
            Console.CursorLeft = xPos;
            int yPos = Console.WindowHeight-1;
            int yLimit;

            if (Erase)
                yLimit = Console.WindowHeight - ((int)Math.Ceiling(oldPiles[xPos]) + 1);
            else
                yLimit = Console.WindowHeight - (int)Math.Ceiling(piles[xPos]);

            while (yPos >= yLimit) {
                Console.CursorTop = yPos;
                if (yPos == yLimit) {
                    double partialFlake = piles[xPos] - Double.Truncate(piles[xPos]);
                    char flake = Snowflake.FlakeModels[(int)Double.Truncate(partialFlake * Snowflake.FlakeModels.Length)];
                    PrintLayerdChar(flake);
                } else
                    PrintLayerdChar(Snowflake.FlakeModels[^1]);
                Console.CursorLeft -= 1;
                yPos--;
            }
                    
            return yPos;
        }

        /// <summary>
        /// Draws the last smoothing layer above the snow pile. Doesn't work to well but It's acceptable.
        /// </summary>
        /// <param name="TopOfPile">The Y layer of the top-most piece of snow pile</param>
        /// <param name="Pos">The X position of the snow pile</param>
        private void DrawSmoothingLayer(int TopOfPile, int Pos) {
            Console.CursorTop = TopOfPile;

            //Here we get the heights of left, current, and right snow pile. This wraps around the screen.
            int lHight = (int)Math.Ceiling(Pos != 0 ? piles[Pos - 1] : piles[^1]);
            int cHight = (int)Math.Ceiling(piles[Pos]);
            int rHight = (int)Math.Ceiling(Pos != Console.WindowWidth - 2 ? piles[Pos + 1] : piles[Console.WindowWidth - 2]);

            //And the logic to determine which symbol to draw. The only time we don't draw an _ symbol, is if it's a 
            //Through, or there's a higher level to the left/right.
            if (cHight < rHight && cHight < lHight)
                PrintLayerdChar('V');
            else if (cHight < rHight)
                PrintLayerdChar('/');
            else if (cHight < lHight)
                PrintLayerdChar('\\');
            else
                PrintLayerdChar('_');
        }

        /// <summary>
        /// Draws a char at the correct layer. Will be used in the future for multi-layerd piles.
        /// </summary>
        /// <param name="ToPrint"></param>
        private void PrintLayerdChar(char ToPrint) {
            var layer = (short)((5 / pileLayer) * 255);
            Console.Write("\x1b[38;2;" + layer + ";" + layer + ";" + layer + "m" + ToPrint);
        }

        /// <summary>
        /// Used to update the snow pile lists if the screen gets resized.
        /// </summary>
        public void UpdateScreenSize() {
            if (Console.WindowWidth - 1 > piles.Count)
                for (int i = piles.Count; i < Console.WindowWidth - 1; i++)
                    piles.Add(0);
            else
                piles = new List<double>(piles.GetRange(0, Console.WindowWidth - 1));

            oldPiles = new List<double>(piles);
        }
    }

    class Program {
        static void Main() { 
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;

            SnowPile pile = new();
            //List<SnowPile> piles = new();
            List<Snowflake> flakes = new();
            Random rng = new();
            Snowflake tempFlake = null;
            int maxFlakes = Console.BufferWidth;

            /*for (int i = 1; i < 6; i++){
                piles.Add(new SnowPile(i));
            }*/

            //generate 100 random flakes in layer order so the top-most layer is always "drawn" on top.
            //This is split evenly into the five layers because RNG isn't super reliable.
            for (int i = 0; i < maxFlakes; i++) {
                //tempFlake = new Snowflake(i, 0, (short)(i % 20), i%(Snowflake.FlakeModels.Length));
                var t = i % 20;
               
                tempFlake = new Snowflake(-1, -1, (short)(t), -1);
                tempFlake.Display();

                //Hook into each flakes "reach bottom" event so we can add them to the snow pile.
                tempFlake.ReachedBottom += (o, e) => {
                    //piles[i%5].AddFlake(o as Snowflake);
                    pile.AddFlake(o as Snowflake);
                };

                flakes.Add(tempFlake);
            }

            //wind variables.
            double wind = 0;
            double maxWind = 0;
            bool spoolWind = false;
            double windSpoolFactor = 0;

            //Ticks used solely to determine when to melt snowflakes.
            int tick = 0;
            //While try loop. It's impossible to detect if the console window was resized, so it will always cause race conditions/io exceptions. 
            //Here we use the io exceptions to our advantage to resize our snow pile lists and refresh the screen.
            while (true) {     
                try {
                    //if we don't have wind, have a chance to make wind
                    if (wind == 0) {
                        if (rng.Next(0, 100) == 99) {
                            maxWind = rng.Next(0, 4);
                            maxWind = rng.Next(0, 2) == 0 ? maxWind : -maxWind; //determine if it's blowing left or right
                            windSpoolFactor = 0.05;// (double)rng.Next(1, 2)/10.0; //how quickly it should spool up.
                            spoolWind = true;
                        }
                    }
                    //if we're spooling wind, use the spool factor to increase strength every tick
                    if (spoolWind) {
                        if (maxWind > 0 && wind < maxWind) {
                            wind += windSpoolFactor;
                        } else if (maxWind < 0 && wind > maxWind) {
                            wind -= windSpoolFactor;
                        } else {
                            spoolWind = false;
                        }
                        //otherwise do the opposite
                    } else if (!spoolWind) {
                        if (maxWind > 0) {
                            if (wind > 0)
                                wind -= windSpoolFactor;
                            else
                                wind = 0;
                        } else if (maxWind < 0) {
                            if (wind < 0)
                                wind += windSpoolFactor;
                            else
                                wind = 0;
                        }
                    }

                   // foreach (SnowPile pile in piles) {
                        //Melt a snow block every 10 loops. 
                        //Since it takes 4 flakes to make one block this seems to keep up fairly well on a standard console screen.
                        //Though I haven't ran this long enough to fully test it, nor do I intend to.

                        if (tick % 2 == 0)
                            pile.Melt();

                        pile.Display();
                   // }

                    foreach (var flake in flakes.ToArray()) {
                        /*double depth = Console.WindowHeight - 1;
                        foreach (SnowPile pile in piles) {
                            double tempDepth = pile.DepthAtFlake(flake);
                            if (tempDepth < depth)
                                depth = tempDepth;
                        }
                        flake.Tick(wind, depth);*/
                        flake.Tick(wind, pile.DepthAtFlake(flake));
                        flake.Display();
                    }

                    tick++;
                } catch (ArgumentOutOfRangeException) {
                    Console.Clear();
                    Console.CursorVisible = false;
                    //foreach (SnowPile pile in piles)
                        pile.UpdateScreenSize();
                }
            }
        }
    }
}