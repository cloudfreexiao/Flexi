namespace Physalia.Flexi.Samples.CardGame
{
    [NodeCategory("Card Game Sample")]
    public class PlayerNode : DefaultValueNode
    {
        public Outport<int> manaRecoverPort;
        public Outport<int> drawCountPort;

        protected override void EvaluateSelf()
        {
            Player player = Container.Game.Player;

            int manaRecover = player.GetStat(StatId.MANA_RECOVER).CurrentValue;
            manaRecoverPort.SetValue(manaRecover);

            int drawCount = player.GetStat(StatId.DRAW).CurrentValue;
            drawCountPort.SetValue(drawCount);
        }
    }
}
