﻿using Asteroids.Exceptions;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Asteroids.SpaceObjects
{
    internal abstract class Asteroid : SpaceObject
    {
        private List<IColliding> CollisionsList;

        private bool Destroyed;

        protected Image AsteroidImage;
        protected Random Random;

        public Asteroid(Point leftTopPosition) : base(leftTopPosition)
        {
            if (LeftTopPosition.X < 0 || LeftTopPosition.Y < 0 || LeftTopPosition.X < Game.Width/2 || LeftTopPosition.Y > Game.Height)
            {
                throw new InvalidSpaceObjectException("Initial position of asteroid can't be in the middle of screen or out of top or bottom of screen");
            }

            Random = new Random();

            MoveDirection = GetMoveDirection();
            AsteroidImage = GetAsteroidImage();
            Health = GetAsteroidMaxHealth();

            Size = new Size(AsteroidImage.Width, AsteroidImage.Height);

            Destroyed = false;

            CollisionsList = new List<IColliding>();
        }

        protected abstract Point GetMoveDirection();
        protected abstract Image GetAsteroidImage();
        protected abstract int GetAsteroidMaxHealth();

        public override void Draw()
        {
            if (Destroyed)
            {
                return;
            }

            Game.Buffer.Graphics.FillEllipse(Brushes.White, BodyShape);
            Game.Buffer.Graphics.DrawImage(AsteroidImage, new Point(LeftTopPosition.X, LeftTopPosition.Y));
        }

        public override bool IsCollidedWithObject(IColliding obj)
        {
            if (
                (!(obj is Bullet) && !(obj is SpaceShip))
                || Destroyed
                || CollisionsList.Contains(obj))
            {
                return false;
            }

            return BodyShape.IntersectsWith(obj.BodyShape);
        }

        public override void OnCollideWithObject(IColliding obj)
        {
            if (!IsCollidedWithObject(obj))
            {
                return;
            }

            if (!CollisionsList.Contains(obj))
            {
                CollisionsList.Add(obj);
            }

            Health -= obj.GetDamage();

            obj.OnCollideWithObject(this);

            if (Health <= 0)
            {
                DestroySpaceObject();
            }
        }

        public override void Update()
        {
            if (Destroyed)
            {
                InitNewSpaceObject();
            }

            LeftTopPosition.X -= MoveDirection.X;

            if (LeftTopPosition.X < -Size.Width)
            {
                DestroySpaceObject();
            }
        }

        private void DestroySpaceObject()
        {
            Destroyed = true;

            LeftTopPosition.X = -1000;
        }

        private void InitNewSpaceObject()
        {
            Destroyed = false;

            Health = GetAsteroidMaxHealth();

            LeftTopPosition.X = Game.Width + 2 * Size.Width;

            LeftTopPosition.Y += Random.Next(-5, 5) * MoveDirection.Y;

            if (LeftTopPosition.Y > Game.Height)
            {
                LeftTopPosition.Y -= Game.Height;
            }
        }

        public override int GetDamage()
        {
            return Health;
        }
    }
}
