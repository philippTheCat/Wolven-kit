using WolvenKit.RED4.CR2W.Reflection;
using FastMember;
using static WolvenKit.RED4.CR2W.Types.Enums;

namespace WolvenKit.RED4.CR2W.Types
{
	[REDMeta]
	public class ForcedKnockdownEvents : KnockdownEvents
	{
		private CBool _firstUpdate;

		[Ordinal(10)] 
		[RED("firstUpdate")] 
		public CBool FirstUpdate
		{
			get => GetProperty(ref _firstUpdate);
			set => SetProperty(ref _firstUpdate, value);
		}

		public ForcedKnockdownEvents(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }
	}
}
