﻿using Asteroids.Exceptions;
using Asteroids.Services;
using Asteroids.SpaceObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asteroids
{
    internal class Game
    {
        /// <summary>
        /// Max pixels for window width and height
        /// </summary>
        private const int GameWindowMaxSize = 1000;

        //TODO: think about levels and asteroids count
        private const int MaxSmallEnergyPacksCount = 3;
        private const int MaxBigAsteroidsCount = 1;
        private const int MaxMediumAsteroidsCount = 2;
        private const int MaxSmallAsteroidsCount = 3;
        private const int WindowGridColsCount = 10;
        private const int WindowGridRowsCount = 10;
        private static BufferedGraphicsContext BufferedGraphicsContext;
        public static BufferedGraphics Buffer;

        private static List<SpaceObject> SpaceObjectList;
        private static List<Asteroid> AsteroidList;
        private static List<Bullet> BulletList;
        private static SpaceShip SpaceShip;

        private static readonly Random Random;
        private static Timer Timer = new Timer();
        private static ILogger Logger = ApplicationLogging.CreateLogger<Game>();

        //Game window dimentions
        public static int Width { get; set; }
        public static int Height { get; set; }

        //Game controls state
        private static bool ControlIsPressedMoveLeft = false;
        private static bool ControlIsPressedMoveRight = false;
        private static bool ControlIsPressedMoveUp = false;
        private static bool ControlIsPressedMoveDown = false;
        private static bool ControlIsPressedFire = false;

        private static bool GameOnPause = false;

        private static int AsteroidsCountIncrement = 0;
        public static int AsteroidsLeft = 0;

        static Game()
        {
            Random = new Random();
            SpaceObjectList = new List<SpaceObject>();
            AsteroidList = new List<Asteroid>();
            BulletList = new List<Bullet>();
        }

        public static void Init(Form form)
        {
            Width = form.ClientSize.Width;
            Height = form.ClientSize.Height;
            ValidateGameWindowSize(Width, Height);

            LoadSpaceObjects();

            BufferedGraphicsContext = BufferedGraphicsManager.Current;
            Buffer = BufferedGraphicsContext.Allocate(form.CreateGraphics(), new Rectangle(0, 0, Width, Height));

            InitFrameUpdateTimer();
            InitKeyListeners(form);
        }

        private static void ValidateGameWindowSize(int width, int height)
        {
            if (
                width > GameWindowMaxSize
                || width < 0
                || height > GameWindowMaxSize
                || height < 0
                )
            {
                throw new ArgumentOutOfRangeException($"Game window width/height values must be in range [0,{GameWindowMaxSize}]");
            }
        }

        private static void InitFrameUpdateTimer()
        {
            Timer.Interval = 60;

            Timer.Tick += TimerTick;

            Timer.Start();
        }

        private static void InitKeyListeners(Form form)
        {
            form.KeyDown += (object sender, KeyEventArgs e) =>
            {
                if (e.KeyCode == Keys.Space)
                {
                    ControlIsPressedFire = true;
                }
                if (e.KeyCode == Keys.Left)
                {
                    ControlIsPressedMoveLeft = true;
                }
                if (e.KeyCode == Keys.Right)
                {
                    ControlIsPressedMoveRight = true;
                }
                if (e.KeyCode == Keys.Up)
                {
                    ControlIsPressedMoveUp = true;
                }
                if (e.KeyCode == Keys.Down)
                {
                    ControlIsPressedMoveDown = true;
                }
                if (e.KeyCode == Keys.P)
                {
                    if (GameOnPause)
                    {
                        GameOnPause = false;
                        Timer.Start();

                    }
                    else
                    {
                        GameOnPause = true;
                        WriteGameMessage("Paused... press P for continue");
                        Timer.Stop();
                    }
                }
            };
            form.KeyUp += (object sender, KeyEventArgs e) =>
            {
                if (e.KeyCode == Keys.Space)
                {
                    ControlIsPressedFire = false;
                }
                if (e.KeyCode == Keys.Left)
                {
                    ControlIsPressedMoveLeft = false;
                }
                if (e.KeyCode == Keys.Right)
                {
                    ControlIsPressedMoveRight = false;
                }
                if (e.KeyCode == Keys.Up)
                {
                    ControlIsPressedMoveUp = false;
                }
                if (e.KeyCode == Keys.Down)
                {
                    ControlIsPressedMoveDown = false;
                }
            };
        }

        private static void ProcessGameControls()
        {
            if (ControlIsPressedFire)
            {
                try
                {
                    Bullet Bullet = SpaceShip.Shoot();
                    BulletList.Add(Bullet);
                    SpaceObjectList.Add(Bullet);
                }
                catch (OutOfAmmoException exception)
                {
                    // do nothing
                    Logger.LogWarning(exception, exception.Message);
                }
            }
            if (ControlIsPressedMoveLeft)
            {
                SpaceShip.MoveHorizontal(-1);
            }
            if (ControlIsPressedMoveRight)
            {
                SpaceShip.MoveHorizontal(1);
            }
            if (ControlIsPressedMoveUp)
            {
                SpaceShip.MoveVertical(-1);
            }
            if (ControlIsPressedMoveDown)
            {
                SpaceShip.MoveVertical(1);
            }
        }

        /// <summary>
        /// Draw game objects
        /// </summary>
        public static void Draw()
        {
            Buffer.Graphics.Clear(Color.Black);
            foreach (SpaceObject spaceObject in SpaceObjectList.ToArray())
            {
                spaceObject.Draw();
            }

            foreach (SpaceObject spaceObject in SpaceObjectList.ToArray())
            {
                if (spaceObject is IColliding)
                {
                    foreach (SpaceObject anotherSpaceObject in SpaceObjectList.ToArray())
                    {
                        if (
                            anotherSpaceObject is IColliding
                            && !object.ReferenceEquals(spaceObject, anotherSpaceObject)
                            && spaceObject.IsCollidedWithObject(anotherSpaceObject)
                            )
                        {
                            spaceObject.OnCollideWithObject(anotherSpaceObject);
                        }
                    }
                }
            }
            Buffer.Render();
        }

        private static void LoadSpaceObjects()
        {
            //TODO: optimize objects spreading
            LoadBackgroundStars();

            SpaceObjectList.Add(new Ufo(new Point(Width / 2, 0)));

            SpaceShip = new SpaceShip(new Point(0, Height / 2));
            SpaceShip.MessageDie += Finish;
            SpaceObjectList.Add(SpaceShip);

            SpawnAsteroids(MaxBigAsteroidsCount, MaxMediumAsteroidsCount, MaxSmallAsteroidsCount);

            SmallEnergyPack SmallEnergyPack = new SmallEnergyPack(new Point(Width, Random.Next(0, Height)));
            SpaceObjectList.Add(SmallEnergyPack);
        }

        private static void SpawnAsteroids(int maxBigAsteroidsCount, int maxMediumAsteroidsCount, int maxSmallAsteroidsCount)
        {
            SpawnAsteroids((Point leftTopPosition) => { return new BigAsteroid(leftTopPosition); }, maxBigAsteroidsCount);
            SpawnAsteroids((Point leftTopPosition) => { return new MediumAsteroid(leftTopPosition); }, maxMediumAsteroidsCount);
            SpawnAsteroids((Point leftTopPosition) => { return new SmallAsteroid(leftTopPosition); }, maxSmallAsteroidsCount);

            AsteroidsLeft = AsteroidList.Count;
        }

        private static void SpawnAsteroids(Func<Point, Asteroid> createAsteroid, int MaxCount)
        {
            for (var i = 0; i < MaxCount; i++)
            {
                var asteroid = createAsteroid(new Point(Width, Random.Next(0, Height)));
                asteroid.AsteroidDestructionEvent += AsteroidIsDestroyed;
                asteroid.AsteroidDestructionEvent += SpaceShip.CalculateScore;

                AsteroidList.Add(asteroid);
            }

            foreach (var asteroid in AsteroidList)
            {
                SpaceObjectList.Add(asteroid);
            }
        }

        private static void LoadBackgroundStars()
        {
            for (int startX = 1, i = 1; startX < Width; startX += Width / WindowGridColsCount, i++)
            {
                for (int startY = 1, j = 1; startY < Height; startY += Height / WindowGridRowsCount, j++)
                {
                    if ((j + i) % 3 == 0)
                    {
                        SpaceObjectList.Add(
                            new PulsingConstellation(
                                new Point(startX, startY),
                                new Point(Random.Next(10, 20), Random.Next(5, 15))
                            )
                        );
                    }
                    else
                    {
                        SpaceObjectList.Add(
                            new XStar(
                                new Point(startX, startY),
                                new Point(Random.Next(5, 15), Random.Next(10, 20)),
                                new Size(3, 3)
                            )
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Update state of every object in game
        /// </summary>
        private static void Update()
        {
            foreach (SpaceObject obj in SpaceObjectList)
            {
                obj.Update();
            }
        }

        private static void TimerTick(object sender, EventArgs e)
        {
            ProcessGameControls();
            Draw();
            Update();
        }

        /// <summary>
        /// Finish game logic
        /// </summary>
        public static void Finish()
        {
            Timer.Stop();
            WriteGameMessage("Ship destroyed!!! The End...");
        }

        public async static void AsteroidIsDestroyed(Asteroid asteroid)
        {
            AsteroidList.Remove(asteroid);
            SpaceObjectList.Remove(asteroid);
            AsteroidsLeft--;

            if (AsteroidList.Count == 0)
            {
                AsteroidsCountIncrement++;
                
                WriteGameMessage("Get ready for next wave!");
                Timer.Stop();
                await Task.Delay(5000);
                Timer.Start();
                SpawnAsteroids(MaxBigAsteroidsCount + AsteroidsCountIncrement, MaxMediumAsteroidsCount + AsteroidsCountIncrement, MaxSmallAsteroidsCount + AsteroidsCountIncrement);
            }
        }

        private static void WriteGameMessage(string message)
        {
            Font Font = new Font(FontFamily.GenericSansSerif, 30, FontStyle.Underline);
            SizeF MessageSize = Buffer.Graphics.MeasureString(message, Font);
            Buffer.Graphics.DrawString(message, Font, Brushes.White, (Width - MessageSize.Width) / 2, Height / 2);
            Buffer.Render();
        }
    }
}
