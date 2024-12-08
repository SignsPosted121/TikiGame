using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
	public interface IFactions
	{
		public enum Faction { NONE, WILD, PLAYER, SHAKERS }

		public bool IsSameFaction(Faction faction);
	}
}
