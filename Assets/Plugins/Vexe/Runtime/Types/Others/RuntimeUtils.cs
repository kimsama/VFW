﻿using Vexe.Runtime.Extensions;

namespace Vexe.Runtime.Types
{ 
	public static class RuntimeUtils
	{
		public static void ResetTarget(object target)
		{
			var type = target.GetType();
#if DBG
			Log("Assigning default values if any to " + type.Name);
#endif
			var members = RuntimeMember.EnumerateMemoized(type);
			for (int i = 0; i < members.Count; i++)
			{
				var member = members[i];
				var defAttr = member.Info.GetCustomAttribute<DefaultAttribute>();
				if (defAttr != null)
				{ 
					member.Target = target;
					var value = defAttr.Value;
					if (value == null && !member.Type.IsAbstract) // null means to instantiate a new instance
						value = member.Type.ActivatorInstance();
					member.Set(value);
				}
			}
		}
	}
}