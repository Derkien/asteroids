﻿using System.Drawing;

namespace Asteroids.SpaceObjects
{
    internal interface IColliding
    {
        /// <summary>
        /// Rectangle parameters of collidable objects
        /// </summary>
        Rectangle BodyShape { get; }

        /// <summary>
        /// Detect if object type suitable for collision with current object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool IsObjectTypeValidForCollision(IColliding obj);

        /// <summary>
        /// Detect is current object collide with given
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool IsCollidedWithObject(IColliding obj);

        /// <summary>
        /// Do something when collision is detected
        /// NOTE: collision should be checked before
        ///       inverse side should call this method too
        /// </summary>
        /// <param name="obj"></param>
        void OnCollideWithObject(IColliding obj);

        /// <summary>
        /// Amount of damage/health given by collided object
        /// </summary>
        /// <returns></returns>
        int GetPower();
    }
}
