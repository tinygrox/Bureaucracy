using System.Linq;

namespace Bureaucracy
{
    public class FireEvent : RandomEventBase
    {
        private readonly BureaucracyFacility facilityToBurn;
        public FireEvent()
        {
            facilityToBurn = FacilityManager.Instance.Facilities.ElementAt(Utilities.Instance.Randomise.Next(0, FacilityManager.Instance.Facilities.Count));
            Title = "火灾！"; // Fire!
            Body = "最近的经费削减导致了 " + facilityToBurn.Name + "消防安全性很差. 结果导致了一小撮火焰最终演变成了熊熊大火。"; // 
            AcceptString = "啊不要啊。 (" + facilityToBurn.Name + "已被摧毁)"; //  is destroyed
            CanBeDeclined = false;
        }
        public override bool EventCanFire()
        {
            if (Utilities.Instance.Randomise.NextDouble() > FacilityManager.Instance.FireChance) return false;
            if (facilityToBurn.IsDestroyed()) return false;
            return facilityToBurn.CanBeDestroyed();
        }

        protected override void OnEventAccepted()
        {
            facilityToBurn.DestroyBuilding();
        }

        protected override void OnEventDeclined()
        {
            
        }
    }
}