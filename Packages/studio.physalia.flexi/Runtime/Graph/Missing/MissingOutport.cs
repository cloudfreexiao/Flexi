using System;

namespace Physalia.Flexi
{
    internal sealed class MissingOutport : Outport, IIsMissing
    {
        public override Type ValueType => Missing.TYPE;

        public MissingOutport(Node node, string name, bool isDynamic) : base(node, name, isDynamic)
        {

        }

        internal override object GetValueBoxed()
        {
            return null;
        }

        protected override bool CanConnectTo(Port port)
        {
            return false;
        }

        internal override bool TryGetConvertedValue<TTo>(out TTo result)
        {
            result = default;
            return false;
        }

        internal override void SetValueFromInport(Inport inport)
        {

        }
    }
}
