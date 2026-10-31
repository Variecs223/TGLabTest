namespace TGLabTest.Simulation
{
    public readonly struct ControlPositions
    {
        public readonly float Collective;
        public readonly float CyclicLateral;
        public readonly float CyclicLongitudinal;
        public readonly float Pedals;

        public ControlPositions(float collective, float cyclicLateral, float cyclicLongitudinal, float pedals)
        {
            Collective = collective;
            CyclicLateral = cyclicLateral;
            CyclicLongitudinal = cyclicLongitudinal;
            Pedals = pedals;
        }
    }
}