namespace Vexe.Runtime.Types.Examples
{
	[BasicView]
	public class ReadonlyFieldsExample : BetterBehaviour
	{
		public readonly int publicField;
		[Serialize] private readonly float privateField;
		[Serialize] protected readonly float protectedField;
	}
}