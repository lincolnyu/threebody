namespace ThreeBodyEngine
{
    public class BodyRecord
    {
        #region Properties

        /// <summary>
        ///  1E12 m as 1.0
        /// </summary>
        public Vector Position { get; set; }

        /// <summary>
        ///  in 1E3 m/s
        /// </summary>
        public Vector Velocity { get; set; }

        #endregion

        #region Methods

        public void CopyFrom(SphericalCelestialBody body)
        {
            Position = body.Position.Clone();
            Velocity = body.Velocity.Clone();
        }

        #endregion
    }
}
