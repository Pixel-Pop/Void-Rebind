using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Cil;
using System;
using System.Linq;
using System.Collections.Generic;
using Rewired;


namespace MonoMod
{

	[MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDrawBinding))]
	class PatchDrawBinding : Attribute { }

	static class MonoModRules
	{

		static MonoModRules()
		{
			Console.WriteLine("Patching Void Bastards");
		}

		public static void PatchDrawBinding(MethodDefinition method, CustomAttribute attrib)
		{
			Console.WriteLine("Patching drawBinding");
			if (method.HasBody)
			{
				ILCursor cursor = new ILCursor(new ILContext(method));
				TypeDefinition type = method.DeclaringType;

				FieldDefinition listMouse = type.Fields.First(tmp => tmp.Name.Equals("listMouse"));
				MethodReference addRange = type.Module.ImportReference(typeof(List<ActionElementMap>).GetMethod("AddRange"));

				/*
				 * 	list.AddRange(listMouse);
				 */

				cursor.TryGotoNext(MoveType.After, instr => instr.MatchPop());
				cursor.Emit(OpCodes.Ldloc_S, (byte)4); // Load list
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldfld, listMouse);
				cursor.Emit(OpCodes.Callvirt, addRange);

				MethodReference op_Equality = type.Module.ImportReference(typeof(string).GetMethod("op_Equality"));
				MethodReference get_Item = type.Module.ImportReference(typeof(List<ActionElementMap>).GetMethod("get_Item"));
				MethodReference get_elementIdentifierName = type.Module.ImportReference(typeof(ActionElementMap).GetMethod("get_elementIdentifierName"));
				Instruction patchEnd = cursor.IL.Create(OpCodes.Ldloc_S, (byte)7);

				/*
				 *	if (safe == "None")
				 *  {
				 *		safe = list[i].elementIdentifierName;
				 *	}
				 */

				cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdloc(7));
				cursor.Emit(OpCodes.Ldstr, "None");
				cursor.Emit(OpCodes.Call, op_Equality);
				cursor.Emit(OpCodes.Brfalse_S, patchEnd);
				cursor.Emit(OpCodes.Ldloc_S, (byte)4); // Load list
				cursor.Emit(OpCodes.Ldloc_S, (byte)6); // Load i
				cursor.Emit(OpCodes.Callvirt, get_Item);
				cursor.Emit(OpCodes.Callvirt, get_elementIdentifierName);
				cursor.Emit(OpCodes.Stloc_S, (byte)7); // Store safe
				cursor.IL.InsertBefore(cursor.Next, patchEnd); // Load safe

				/* Patch loop to show 3 columns instead of 2 */
				cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Blt);
				cursor.Prev.OpCode = OpCodes.Ldc_I4_3;
				cursor.TryGotoNext(MoveType.Before, instr => instr.OpCode == OpCodes.Blt);
				cursor.Prev.OpCode = OpCodes.Ldc_I4_3;

				/* Change behavior when empty box is clicked */
				cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchNewobj("Rewired.InputMapper/Context"));
				while (cursor.Next.OpCode != OpCodes.Pop)
                {
					cursor.Remove();
				}
				cursor.Remove();
				cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld("KeyboardOptionsScreen", "currentButtonId"));
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Callvirt, type.Methods.First(tmp => tmp.Name == "drawBindingEmptyBox"));

				/* Change behavior when box with mapping is clicked */
				cursor.TryGotoPrev(MoveType.Before, instr => instr.MatchNewobj("Rewired.InputMapper/Context")); // Other call was removed
				while (cursor.Next.OpCode != OpCodes.Pop)
				{
					cursor.Remove();
				}
				cursor.Remove();
				cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld("KeyboardOptionsScreen", "currentButtonId"));
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldloc_S, (byte)4); // Load list
				cursor.Emit(OpCodes.Ldloc_S, (byte)6); // Load i
				cursor.Emit(OpCodes.Callvirt, get_Item);
				cursor.Emit(OpCodes.Callvirt, type.Methods.First(tmp => tmp.Name == "drawBindingFullBox"));

				cursor.Body.Optimize();
			}
			else
			{
				Console.WriteLine("Error patching drawBinding");
				throw new Exception();
			}
		}
	}
}