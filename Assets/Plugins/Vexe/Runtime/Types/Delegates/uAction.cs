﻿using System;
using Vexe.Runtime.Extensions;

namespace Vexe.Runtime.Types
{
	public class uAction : uBaseDelegate<Action>
	{
		public override Type[] ParamTypes
		{
			get { return Type.EmptyTypes; }
		}

		public override Type ReturnType
		{
			get { return typeof(void); }
		}

		public void Invoke()
		{
			Value.SafeInvoke();
		}

		protected override void DirectAdd(Action handler)
		{
			directValue += handler;
		}

		protected override void DirectRemove(Action handler)
		{
			directValue -= handler;
		}

		protected override string GetHandlerMethod(Action handler)
		{
			return handler.Method.Name;
		}

		protected override object GetHandlerTarget(Action handler)
		{
			return handler.Target;
		}
	}

	public class uAction<T0> : uBaseDelegate<Action<T0>>
	{
		public override Type[] ParamTypes
		{
			get { return new[] { typeof(T0) }; }
		}

		public override Type ReturnType
		{
			get { return typeof(void); }
		}

		public void Invoke(T0 arg0)
		{
			Value.SafeInvoke(arg0);
		}

		protected override void DirectAdd(Action<T0> handler)
		{
			directValue += handler;
		}

		protected override void DirectRemove(Action<T0> handler)
		{
			directValue -= handler;
		}

		protected override string GetHandlerMethod(Action<T0> handler)
		{
			return handler.Method.Name;
		}

		protected override object GetHandlerTarget(Action<T0> handler)
		{
			return handler.Target;
		}
	}

	public class uAction<T0, T1> : uBaseDelegate<Action<T0, T1>>
	{
		public override Type[] ParamTypes
		{
			get { return new[] { typeof(T0), typeof(T1) }; }
		}

		public override Type ReturnType
		{
			get { return typeof(void); }
		}

		public void Invoke(T0 arg0, T1 arg1)
		{
			Value.SafeInvoke(arg0, arg1);
		}

		protected override void DirectAdd(Action<T0, T1> handler)
		{
			directValue += handler;
		}

		protected override void DirectRemove(Action<T0, T1> handler)
		{
			directValue -= handler;
		}

		protected override string GetHandlerMethod(Action<T0, T1> handler)
		{
			return handler.Method.Name;
		}

		protected override object GetHandlerTarget(Action<T0, T1> handler)
		{
			return handler.Target;
		}
	}

	public class uAction<T0, T1, T2> : uBaseDelegate<Action<T0, T1, T2>>
	{
		public override Type[] ParamTypes
		{
			get { return new[] { typeof(T0), typeof(T1), typeof(T2) }; }
		}

		public override Type ReturnType
		{
			get { return typeof(void); }
		}

		public void Invoke(T0 arg0, T1 arg1, T2 arg2)
		{
			Value.SafeInvoke(arg0, arg1, arg2);
		}

		protected override void DirectAdd(Action<T0, T1, T2> handler)
		{
			directValue += handler;
		}

		protected override void DirectRemove(Action<T0, T1, T2> handler)
		{
			directValue -= handler;
		}

		protected override string GetHandlerMethod(Action<T0, T1, T2> handler)
		{
			return handler.Method.Name;
		}

		protected override object GetHandlerTarget(Action<T0, T1, T2> handler)
		{
			return handler.Target;
		}
	}
}