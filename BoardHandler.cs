﻿using System;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Linq;
using System.Xml;

namespace Game
{
    public enum BoardTile
    {
        Empty = 0, // Air
        Wall = -1, // Wall
        Ball = -2, // Ball
        BouncerLeft = -3, // Bouncer left
        BouncerCenter = -4, // Bouncer middle
        BouncerRight = -5, // Bouncer right
        Poof = -6 // Destruction particle

    }

    public enum Status
    {
        Alive = 0,
        Dead = 1,
        Victory = 2
    }

    public class BoardHandler
    {


        // Board information
        private int[,] layout; // The layout of the board, represented as [x,y]
        private int boardWidth; // width of the layout
        private int boardHeight; // height of the layout
        private int totalBlocks = 0; // Total value of all the breakable blocks on the map

        //Current state of the game board
        private int difficultyLevel = 1; // Value that determines toughness of blocks
        Status gameState = Status.Alive; // State of game
        int score = 0; // Accumulated score

        // Bouncer
        int bouncerLocation; // The x position of the center of the Bouncer on the board

        // Ball details
        int[] ballLocation; // The location of the ball on the board
        bool movement = false; // Whether the ball is freely moving or not
        int yDirection = 0; // Movement in y-axis
        int xDirection = 0; // Movement in x-axis
        private enum Angle
        {
            None = 0, LowAngle = 1, NormalAngle = 2, HighAngle = 3, // Higher angle means a steeper trajectory, None for vertical movement
        }
        Angle ballAngle;
        int ballTime; // The time in updates the ball has been in movement

        /// <summary>
        /// The BoardHandler object is instantiated with the width, height, and difficulty level of the board.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="difficulty"></param>
        public BoardHandler(int width, int height, int difficulty)
        {
            layout = new int[width, height];
            boardWidth = width;
            boardHeight = height;
            difficultyLevel = difficulty;
            for (int w = 0; w < width; w++)
            {
                layout[w, 0] = -1;
                if (w == 0 | w == width - 1)
                {
                    for (int h = 1; h < height - 1; h++)
                    {
                        layout[w, h] = -1;
                    }
                }

            }
            bouncerLocation = boardWidth / 2;
            ballLocation = new int[2] { boardWidth / 2, boardHeight - 1 };
            movement = false; yDirection = 0; xDirection = 0; ballAngle = Angle.None; ballTime = 0;

        }

        /// <summary>
        /// Creates a MapBuilder object to insert breakable blocks into the board,
        /// depending on the map chosen by the player. Stores the total value
        /// of blocks placed in totalBlocks.
        /// </summary>
        /// <param name="map"></param>
        public void BuildBoard(int map)
        {
            IMapBuilder builder;
            switch (map)
            {
                case 1:
                    builder = new Map1();
                    break;
                case 2:
                    builder = new Map2();
                    break;
                case 3:
                    builder = new Map3();
                    break;
                case 4:
                    builder = new Map4();
                    break;
                case 5:
                    builder = new Map5();
                    break;
                default:
                    return;
            }

            int areawidth = boardWidth - 2;
            int areaheight = boardHeight - 5;
            builder.ObserveSize(areawidth, areaheight);

            for (int h = 1; h < boardHeight - 3; h++)
            {
                for (int w = 1; w < boardWidth - 1; w++)
                {
                    layout[w,h] = builder.PlaceBlock(difficultyLevel);
                }
            }
            totalBlocks = builder.GetBlocks();
            return;
        }

        /// <summary>
        /// Writes a textual representation of the board to the console
        /// </summary>
        public void DrawBoard()
        {
            
            for (int lines = 0; lines < 1; lines++)
            {
                Console.WriteLine("\n");
            }
            Console.WriteLine("SCORE: {0}", score);
            for (int h = 0; h < boardHeight; h++)
            {
                for (int w = 0; w < boardWidth; w++)
                {
                    switch ((BoardTile)layout[w, h])
                    {
                        case BoardTile.Empty:
                            Console.Write("   ");
                            break;
                        case BoardTile.Wall:
                            Console.Write("[X]");
                            break;
                        case BoardTile.Ball:
                            Console.Write("(O)");
                            break;
                        case BoardTile.BouncerLeft:
                            Console.Write("///");
                            break;

                        case BoardTile.BouncerCenter:
                            Console.Write("===");
                            break;
                        case BoardTile.BouncerRight:
                            Console.Write("\\\\\\");
                            break;
                        case BoardTile.Poof:
                            Console.Write("###");
                            break;
                        default:
                            Console.Write("[{0}]", layout[w, h]);
                            break;
                     }
                }
                Console.Write("\n");
            }
        }
        
        /// <summary>
        /// Updates the values on the board to correspond to the new Ball and Bouncer locations.
        /// Calls BallPhysics to determine the change in position of the Ball.
        /// </summary>
        /// <returns></returns>
        private int UpdateBoard()
        {
            
            // Move the Ball
            
            layout[ballLocation[0], ballLocation[1]] = 0;

            if (movement == false) // Ball is on Bouncer
            {
                ballLocation[0] = bouncerLocation;
                ballLocation[1] = boardHeight - 2;
            }
            else // Ball is moving
            {
                BallPhysics();
            }
            // Set updated Ball position
            layout[ballLocation[0], ballLocation[1]] = -2;


            // Renew Bouncer location
            if (bouncerLocation > 1)
            {
                layout[bouncerLocation - 2, boardHeight - 1] = 0;
            }
            layout[bouncerLocation - 1, boardHeight - 1] = -3;
            layout[bouncerLocation, boardHeight - 1] = -4;
            layout[bouncerLocation + 1, boardHeight - 1] = -5;
            if (bouncerLocation < boardWidth - 2)
            {
                layout[bouncerLocation + 2, boardHeight - 1] = 0;
            }



            return 0;
        }

        /// <summary>
        /// Moves the Bouncer depending on the player's input. Then update the board.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public int Move(int direction)
        {
            if (bouncerLocation + direction > boardWidth - 2)
            {
                return -1;
            } else if (bouncerLocation + direction < 1)
            {
                return -1;
            }
            bouncerLocation += direction;

            UpdateBoard();
            return 0;
        }

        /// <summary>
        /// If the ball is on the Bouncer, send it upwards. Otherwise do nothing.
        /// </summary>
        /// <returns></returns>
        public int Launch()
        {
            if (movement == false)
            {
                ballLocation[1] = boardHeight - 3;
                layout[ballLocation[0], boardHeight - 2] = 0;
                yDirection = -1;
                xDirection = 0;
                movement = true;
                return 0;
            }
            return 0;
        }

        /// <summary>
        /// Determines the new angle and direction of the Ball depending on what it collides with.
        /// Blocks it collides with lose 1 durability and adds points (Win if no blocks left).
        /// Assumes that the Ball is in motion.
        /// </summary>
        /// <returns></returns>
        private int BallPhysics()
        {
            int xpos = ballLocation[0];
            int ypos = ballLocation[1];

            bool yFlip = false;
            bool xFlip = false;
            //
            // If the ball is at the bottom then the Bouncer must be there to catch it
            if (ypos == boardHeight - 2) 
            {
                switch ((BoardTile)layout[xpos, ypos + 1])
                {
                    case BoardTile.BouncerLeft:
                        yFlip = true;
                        xDirection = -1;
                        ballAngle = Angle.NormalAngle;
                        break;
                    case BoardTile.BouncerCenter:
                        yFlip = true;
                        ballAngle = Angle.None;
                        break;
                    case BoardTile.BouncerRight:
                        yFlip = true;
                        xDirection = 1;
                        ballAngle = Angle.NormalAngle;
                        break;
                    case BoardTile.Empty:
                        Death();
                        gameState = Status.Dead;
                        return 0;

                    default:
                        gameState = Status.Dead;
                        return 0;
                        //break;
                }
            }
            else
            {
                if (layout[xpos + xDirection, ypos] != 0 | layout[xpos, ypos + yDirection] != 0) // Check if adjacent tiles are solid
                {
                    if (layout[xpos + xDirection, ypos] == -1) // Check right/left
                    {
                        xFlip = true;
                    }
                    else if (layout[xpos + xDirection, ypos] > 0) // (Breakable)
                    {
                        layout[xpos + xDirection, ypos] -= 1;
                        score += 100;
                        totalBlocks -= 1;
                        xFlip = true;
                    }
                    if (layout[xpos, ypos + yDirection] == -1) // Check up/down
                    {
                        yFlip = true;
                    }
                    else if (layout[xpos, ypos + yDirection] > 0) // (Breakable)
                    {
                        layout[xpos, ypos + yDirection] -= 1;
                        score += 100;
                        totalBlocks -= 1;
                        yFlip = true;
                    }
                }
                else if (layout[xpos + xDirection, ypos + yDirection] > 0) // Adjacent tiles are clear, so check the corners.
                                                                           // Note that this can't be the corner of the board, since 
                                                                           // there is always an adjacent tile if the Ball is at the corner.
                {
                    layout[xpos + xDirection, ypos + yDirection] -= 1;
                    score += 100;
                    totalBlocks -= 1;
                    xFlip = true; yFlip = true;
                }
            }
            if (xFlip) { xDirection *= -1; }
            if (yFlip) { yDirection *= -1; }

            if (totalBlocks == 0) { gameState = Status.Victory; }
            else 
            {
                // Move the ball
                switch (ballAngle)
                {
                    case Angle.None:
                        ballLocation[1] += yDirection;
                        break;

                    case Angle.LowAngle:
                        if (ballTime % 2 == 1)
                        {
                            ballLocation[1] += yDirection;
                        }
                        ballLocation[0] += xDirection;
                        break;

                    case Angle.NormalAngle:
                        ballLocation[0] += xDirection;
                        ballLocation[1] += yDirection;
                        break;

                    case Angle.HighAngle:
                        if (ballTime % 2 == 1)
                        {
                            ballLocation[0] += xDirection;
                        }
                        ballLocation[1] += yDirection;
                        break;

                    default:
                        break;
                }
                ballTime++;
            }
            return 0;

        }

        /// <summary>
        /// Provides a visual of the destruction of the Ball.
        /// </summary>
        private void Death()
        {
            layout[ballLocation[0], ballLocation[1]] = -6;
            layout[ballLocation[0], ballLocation[1] - 1] = -6;
            layout[ballLocation[0], ballLocation[1] + 1] = -6;
            layout[ballLocation[0] - 1, ballLocation[1] + 1] = -6;
            layout[ballLocation[0] + 1, ballLocation[1] + 1] = -6;
            DrawBoard();
            layout[ballLocation[0], ballLocation[1]] = 0;
            layout[ballLocation[0], ballLocation[1] - 1] = 0;
            layout[ballLocation[0], ballLocation[1] + 1] = 0;
            layout[ballLocation[0] - 1, ballLocation[1] + 1] = 0;
            layout[ballLocation[0] + 1, ballLocation[1] + 1] = 0;
            DrawBoard();
        }

        /// <summary>
        /// Resets the status of the board, the locations of the Ball and Bouncer,
        /// and the movement of the Ball.
        /// </summary>
        /// <returns></returns>
        public int Reset()
        {
            gameState = Status.Alive;
            bouncerLocation = boardWidth / 2;
            ballLocation = new int[2] { boardWidth / 2, boardHeight - 1 };
            movement = false; yDirection = 0; xDirection = 0; ballAngle = Angle.None; ballTime = 0;
            UpdateBoard();
            return 0;
        }

        /// <summary>
        /// Returns the current gameState (Alive, Dead, Victory)
        /// </summary>
        /// <returns></returns>
        public Status GetStatus(){
            return gameState;
        }

        // Returns the current Score 
        public int GetScore()
        {
            return score;
        }

    }
}