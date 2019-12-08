using System;
using System.Threading;
using System.Collections.Generic;

//What am I even doing these days. 

namespace ConsoleSnowSim {
    class Snowflake {
        private static readonly Random rng = new Random();
        private static readonly String flakes = ".,*+#";

        private int layer = 1;
        private ConsoleColor shade;
        private double xPos = 0;
        private double yPos = 0;
        private int oldXPos = 0;
        private int oldYPos = 0;
        private bool updateOldPos = true;


        /// <summary>
        /// Fluffiniess of the snowflake. 0-4, 0 being tiny and hard.
        /// </summary>
        public int Weight { set; get; } = 1;

        //This and the height properties will fail if value == 2x WindowWidth, but we'll never do that so we ignore it.
        public double XPos { 
            set {
                if ((int)Math.Round(value) >= Console.WindowWidth)
                    xPos = value - Console.WindowWidth;
                else if ((int)Math.Round(value) < 0)
                    xPos = Console.WindowWidth + value;
                else
                    xPos = value;
            } 
            get { return xPos; } 
        }
        public double YPos {
            set {
                if ((int)Math.Round(value) >= Console.WindowHeight)
                    yPos = value - Console.WindowHeight;
                else if ((int)Math.Round(value) < 0)
                    yPos = Console.WindowHeight + value;
                else
                    yPos = value;
            }
            get { return yPos; } 
        }

        /// <summary>
        /// Initiate a snowflake with random values unless parameters are passed
        /// </summary>
        /// <param name="x">X Position</param>
        /// <param name="y">Y Position</param>
        /// <param name="l">Layer, 1-3</param>
        /// <param name="w">Weight, 0-4</param>
        public Snowflake(int x = -1, int y = -1, int l = -1, int w = -1) {
            XPos = (x == -1 ? rng.Next(0, Console.WindowWidth) : x);
            YPos = (y == -1 ? rng.Next(0, Console.WindowHeight) : y);
            layer = (l == -1 ? rng.Next(1, 4) : l);
            Weight = (w == -1 ? rng.Next(0, flakes.Length) : w);
            switch (layer) {
                case 1:
                    shade = ConsoleColor.White;
                break;
                case 2:
                    shade = ConsoleColor.Gray;
                break;
                case 3:
                    shade = ConsoleColor.DarkGray;
                break;
            }
        }

        /// <summary>
        /// Clear the last snowflake position
        /// </summary>
        private void Erase() {
            Console.CursorLeft = oldXPos;
            Console.CursorTop = oldYPos;
            Console.Write(" ");

            updateOldPos = true;
        }

        /// <summary>
        /// Draw current snowflake position
        /// </summary>
        public void Display() {
            Erase();

            Console.CursorLeft = (int)Math.Round(XPos);
            Console.CursorTop = (int)Math.Round(YPos);

            Console.ForegroundColor = shade;
            Console.Write(flakes[Weight]);
        }

        /// <summary>
        /// Update snowflake position
        /// </summary>
        /// <param name="WindForce">Amount of wind to apply to x position</param>
        public void Tick(double WindForce = 0) {
            //Update old position before over-writing it so we can cleanly erase old snowflake
            if (updateOldPos) {
                oldXPos = (int)Math.Round(xPos);
                oldYPos = (int)Math.Round(yPos);
                updateOldPos = false;
            }

            double add = 0.5;
            //slow flake down for background layers
            if (layer != 1) {
                add -= (0.5 / layer);
            }
            //slow flake down further for bigger "fluffier" flakes
            add += 0.5 / (Weight+1);

            YPos += add;

            //if we have wind, apply it. Less force for background layers, more force for fluffy snowflakes
            if (WindForce != 0)
                XPos += Math.Round(WindForce / layer) - (0.5 / (Weight + 1));

            //Randomly shift the flake around one tile left or right to make it look more natural
            if (rng.Next(0, 50) == 0) {
                XPos += (rng.Next(0, 2) == 0 ? -1 : 1);
            }
        }

        public void Respawn() {
            Erase();
        }

    }

    class Program {
        static void Main(string[] args) {
            Console.CursorVisible = false;
            List<Snowflake> flakes = new List<Snowflake>();

            Random rng = new Random();

            //generate 100 random flakes
            for (int i = 0; i < 100; i++) {
                flakes.Add(new Snowflake());
            }
            //used for debugging movement
            /*int l = 1;
            int w = 0;
            for (int i = 0; i < 119; i++) {
                flakes.Add(new Snowflake(i, 0, l, w));
                if (w == 4) {
                    l = (l == 3 ? 1 : l + 1);
                    w = 0;
                } else
                    w++;
            }
            //flakes.Add(new Snowflake(20, 0, 2, 0));
            //flakes.Add(new Snowflake(30, 0, 3, 0));*/

            //wind variables.
            double wind = 0;
            double maxWind = 0;
            bool spoolWind = false;
            double windSpoolFactor = 0;
            while (true) {
                //if we don't have wind, have a chance to make wind
                if (wind == 0) {
                    if (rng.Next(0, 100) > 75) {
                        maxWind = rng.Next(0, 4);
                        maxWind = (rng.Next(0, 2) == 0 ? maxWind : -maxWind); //determine if it's blowing left or right
                        windSpoolFactor = (double)rng.Next(1, 4)/10.0; //how quickly it should spool up.
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

                foreach (var flake in flakes) {
                    flake.Display();
                    flake.Tick(wind);
                }
                Thread.Sleep(250);
            }

        }
    }
}
