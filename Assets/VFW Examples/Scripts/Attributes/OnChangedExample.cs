﻿using System.Collections.Generic;
using UnityEngine;

namespace Vexe.Runtime.Types.Examples
{
	[BasicView]
	public class OnChangedExample : CachedBehaviour
	{
		// when this string changes, set the `tag` property to the new value and log it
		[Tags, OnChanged(Set = "tag", Call = "Log")] // could have also written OnChanged("Log", Set = "tag")
		public string playerTag;

		// if any vector of this array changes, we set the `position` property to that new vector
		[PerItem, OnChanged(Set = "position")]
		public Vector3[] vectors;

		// if any value of this dictionary changes, set our scale to that value
		// you could use PerKey to apply attributes on the dictionary's keys instead of values
		[PerValue, OnChanged(Set = "localScale")]
		public Dictionary<string, Vector3> dictionary;

		// Note that position and localScale are properties defined in CachedBehaviour
	}
}
