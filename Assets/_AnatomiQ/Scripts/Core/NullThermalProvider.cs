namespace AnatomiQ.Core
{
    /// <summary>
    /// Default <see cref="IThermalProvider"/> used until the real Adaptive Performance adapter is
    /// enabled at CORE-001 (Decision B). Reports no thermal source, so CORE-007's CheckThermal stays
    /// inert: the thermal tier never leaves Nominal, exactly like the AR/API/inference stubs remain
    /// inert until their sources exist. This keeps the editor and PlayMode (where Adaptive
    /// Performance returns nothing) from ever seeing a spurious thermal degradation.
    /// </summary>
    public sealed class NullThermalProvider : IThermalProvider
    {
        /// <inheritdoc />
        public bool IsAvailable => false;

        /// <inheritdoc />
        public ThermalWarning Warning => ThermalWarning.None;

        /// <inheritdoc />
        public float TemperatureLevel => 0f;
    }
}
