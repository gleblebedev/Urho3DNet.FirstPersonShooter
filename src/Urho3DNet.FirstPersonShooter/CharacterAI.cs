namespace Urho3DNet.FirstPersonShooter
{
    [ObjectFactory]
    public class CharacterAI : LogicComponent
    {
        public CharacterAI(Context context):base(context)
        {
            UpdateEventMask = UpdateEvent.UseUpdate;
        }

        public override void Update(float timeStep)
        {
            base.Update(timeStep);
        }
    }
}