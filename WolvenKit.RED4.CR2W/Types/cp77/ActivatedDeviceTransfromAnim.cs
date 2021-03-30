using WolvenKit.RED4.CR2W.Reflection;
using FastMember;
using static WolvenKit.RED4.CR2W.Types.Enums;

namespace WolvenKit.RED4.CR2W.Types
{
	[REDMeta]
	public class ActivatedDeviceTransfromAnim : InteractiveDevice
	{
		private CInt32 _animationState;

		[Ordinal(96)] 
		[RED("animationState")] 
		public CInt32 AnimationState
		{
			get => GetProperty(ref _animationState);
			set => SetProperty(ref _animationState, value);
		}

		public ActivatedDeviceTransfromAnim(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }
	}
}
