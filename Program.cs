using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleSnowSim {
    class Snowflake {
        private static readonly Random rng = new Random();
        private static readonly string flakes = ".,*+#";

        private int layer = 1;
        private ConsoleColor shade;

        private double xPos = 0;
        private double yPos = 0;

        private int oldXPos = 0;
        private int oldYPos = 0;

        private double depthAtFlake = 0;
        private bool updateOldPos = true;

        /// <summary>
        /// Fired when the flake reaches the bottom of the screen.
        /// </summary>
        public event EventHandler ReachedBottom;

        /// <summary>
        /// Fluffiniess of the snowflake. 0-4, 0 being tiny and hard.
        /// </summary>
        public int Weight { set; get; } = 1;

        //These are used by the console so i don't have to keep rounding values in code. THis also keeps flakes on screen.
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
                    ReachedBottom?.Invoke(this, EventArgs.Empty);
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
        public Snowflake(int x = -1, int y = -1, int l = -1, int w = -1) {
            XPos = x == -1 ? rng.Next(0, Console.WindowWidth - 1) : x;
            YPos = y == -1 ? rng.Next(0, Console.WindowHeight) : y;
            layer = l == -1 ? rng.Next(1, 4) : l;
            Weight = w == -1 ? rng.Next(0, flakes.Length) : w;

            shade = layer switch
            {
                1 => ConsoleColor.White,
                2 => ConsoleColor.Gray,
                3 => ConsoleColor.DarkGray,
                _ => ConsoleColor.Black,
            };
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

            Console.CursorLeft = RoundXPos;
            Console.CursorTop = RoundYPos;

            Console.ForegroundColor = shade;
            Console.Write(flakes[Weight]);
        }

        /// <summary>
        /// Update snowflake position
        /// </summary>
        /// <param name="WindForce">Amount of wind to apply to x position</param>
        public void Tick(double WindForce = 0, double depth = 0) {
            depthAtFlake = depth;
            //Update old position before over-writing it so we can cleanly erase old snowflake
            if (updateOldPos || oldXPos >= Console.WindowWidth - 1) {
                oldXPos = RoundXPos;
                oldYPos = RoundYPos;
                updateOldPos = false;
            }

            double add = 0.5;
            //slow flake down for background layers
            if (layer != 1)
                add -= (0.5 / layer);

            //slow flake down further for bigger "fluffier" flakes
            add += 0.5 / (Weight + 1);

            YPos += add;
            //if we have wind, apply it. Less force for background layers, more force for fluffy snowflakes
            if (WindForce != 0)
                XPos += Math.Round(WindForce / layer) - (0.5 / (Weight + 1)); ;

            //Randomly shift the flake around one tile left or right to make it look more natural
            if (rng.Next(0, 50) == 0)
                XPos += rng.Next(0, 2) == 0 ? -1 : 1;
        }
    }

    internal class SnowPile {
        private List<double> piles = new List<double>();
        private List<double> oldPiles;

        /// <summary>
        /// Creates a screen-sized pile with 0 snow
        /// </summary>
        public SnowPile() {
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

            //It takes 4 flakes to make one layer of snow
            piles[pos] += 0.25;

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

            foreach (double pile in piles) {
                //Fully erase the pile if it changed, ie was rolled-over
                if (pile < oldPiles[xPos])
                    DrawPile(xPos, 'x', true);

                //(Re)draw the pile of fully filled snow blocks
                int y = DrawPile(xPos, '#');

                //Draw the last snowblock, it's not fully filled so it'll have this effect.
                char snow = ' ';
                double dec = pile - Math.Truncate(pile);

                if (dec < 0.25)
                    snow = '.';
                else if (dec >= 0.25 && dec < 0.5)
                    snow = '+';
                else if (dec >= 0.5 && dec < 0.75)
                    snow = '%';
                else
                    snow = '#';

                Console.CursorTop = y;
                Console.Write(snow);
                Console.CursorLeft -= 1;

                //Draw the top-most smoothing layer.
                DrawSmoothingLayer(y, xPos);

                xPos++;
            }
            //Update the old pile list for comparisons. 
            oldPiles = new List<double>(piles);
        }

        /// <summary>
        /// Draws a snow pile at the specified pos, using the provided character. If IntoTopLayer is set, will go one more up to over-ride the "smooth" layer.
        /// </summary>
        /// <param name="Pos">X Position of the pile</param>
        /// <param name="ToDraw">What char to use</param>
        /// <param name="IntoTopLayer">Should it also over-write the top-most smoothing layer?</param>
        private int DrawPile(int Pos, char ToDraw, bool IntoTopLayer = false) {
            Console.CursorLeft = Pos;
            int y = Console.WindowHeight - 1;
            int limit = 0;
            if (IntoTopLayer)
                limit = y - (int)Math.Floor(oldPiles[Pos]) - 1;
            else
                limit = y - (int)Math.Floor(piles[Pos]);

            while (y > limit) {
                Console.CursorTop = y;
                Console.Write(ToDraw);
                Console.CursorLeft -= 1;
                y--;
            }
            return y;
        }

        /// <summary>
        /// Draws the last smoothing layer above the snow pile. Doesn't work to well but It's acceptable.
        /// </summary>
        /// <param name="TopOfPile">The Y layer of the top-most piece of snow pile</param>
        /// <param name="Pos">The X position of the snow pile</param>
        private void DrawSmoothingLayer(int TopOfPile, int Pos) {
            Console.CursorTop = TopOfPile - 1;

            //Here we get the heights of left, current, and right snow pile. This wraps around the screen.
            int lHight = (int)Math.Floor(Pos != 0 ? piles[Pos - 1] : piles[piles.Count - 1]);
            int cHight = (int)Math.Floor(piles[Pos]);
            int rHight = (int)Math.Floor(Pos != Console.WindowWidth - 2 ? piles[Pos + 1] : piles[Console.WindowWidth - 2]);
            //And the logic to determine which symbol to draw. Could probably be done more pragmatically but honestly I don't care lol.
            if (cHight == rHight && lHight == cHight)
                Console.Write("_");
            else if (cHight < rHight) {
                if (lHight == cHight || lHight < cHight)
                    Console.Write("/");
                else if (cHight < lHight)
                    Console.Write("V");
                else
                    Console.Write("_");
            } else if (cHight > rHight) {
                if (lHight > cHight)
                    Console.Write("\\");
                else
                    Console.Write("_");
            } else if (cHight < lHight) {
                if (rHight == cHight)
                    Console.Write("\\");
            } else if (cHight > lHight) {
                if (rHight == cHight)
                    Console.Write("_");
            }
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

    /*internal static partial class Console {
    
        public static void Test() {

        }
    }*/

    class Program {
        static void Main() {
            Console.Clear();
            Console.CursorVisible = false;
            //Setting the buffer size to the window size removes the scroll bars.
            Console.BufferHeight = Console.WindowHeight;
            Console.BufferWidth = Console.WindowWidth;

            //Console.ReadKey();
             //   return;


            SnowPile pile = new SnowPile();
            List<Snowflake> flakes = new List<Snowflake>();
            Random rng = new Random();
            Snowflake tempFlake = null;
            int maxFlakes = 100;

            //generate 100 random flakes in layer order so the top-most layer is always "drawn" on top.
            //This is split evenly into the three layers because RNG isn't super reliable.
            for (int i = 0; i < maxFlakes; i++) {
                if (i < maxFlakes / 3)
                    tempFlake = new Snowflake(-1, -1, 3, -1);
                else if (i < (maxFlakes / 3) * 2)
                    tempFlake = new Snowflake(-1, -1, 2, -1);
                else
                    tempFlake = new Snowflake(-1, -1, 1, -1);

                //Hook into each flakes "reach bottom" event so we can add them to the snow pile.
                tempFlake.ReachedBottom += (o, e) => {
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

                    //Melt a snow block every 10 loops. 
                    //Since it takes 4 flakes to make one block this seems to keep up fairly well on a standard console screen.
                    //Though I haven't ran this long enough to fully test it, nor do I intend to.
                    if (tick % 2 == 0)
                        pile.Melt();

                    pile.Display();

                    foreach (var flake in flakes.ToArray()) {
                        flake.Tick(wind, pile.DepthAtFlake(flake));
                        flake.Display();
                    }

                    tick++;
                } catch (ArgumentOutOfRangeException) {
                    Console.Clear();
                    Console.CursorVisible = false;
                    Console.BufferHeight = Console.WindowHeight;
                    Console.BufferWidth = Console.WindowWidth;
                    pile.UpdateScreenSize();
                }
            }
        }
    }
}