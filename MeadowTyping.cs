using System;
using System.Linq;
using BepInEx;
using RainMeadow;
using MonoMod.RuntimeDetour;
using System.Reflection;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Menu;
using Menu.Remix.MixedUI;
using System.Security.Permissions;
using System.Security;
using MonoMod.Utils;
using IL.RWCustom;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MeadowTyping;

[BepInPlugin("yuzugamer.MeadowTyping", "Better Meadow Typing", "1.2.0")]
public partial class MeadowTyping : BaseUnityPlugin
{
    public static bool MeadowTypingInit = false;
    public static int chatcursor = 0;
    public static int arrowheld = 0;
    public static int backspaceheld = 0;
    public static ChatTextBox OpenChat;
    public static ChatHud ActiveChat;
    public static bool checkkeys = false;
    //public static char[] validchars = new char[] { ' ', '!', '.', ',', '"', '\'', ':', ';', '~', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '+', '=', '[', ']', '{', '}', '\\', '|'};
    private static MeadowTyping instance;
    private static FieldInfo chatinputoverlay;
    private static MethodInfo storyresetinput;
    private static MethodInfo storyupdatelog;
    private static FieldInfo blockinput;
    private static FieldInfo storychattoggled;
    

    private void OnEnable()
    {
        instance = this;
        On.RainWorld.PostModsInit += On_RainWorld_PostModsInit;
    }

    private void On_RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);
        if (MeadowTypingInit == false)
        {
            //Logger.LogInfo("Started loading!");
            MeadowTypingInit = true;
            try
            {
                //shutdownrequest = typeof(ChatTextBox).GetField("OnShutDownRequest", BindingFlags.Public | BindingFlags.Static);
                //typinghandler = typeof(ChatTextBox).GetField("typingHandler", BindingFlags.Instance | BindingFlags.NonPublic);
                chatinputoverlay = typeof(ChatHud).GetField("chatInputOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
                storyresetinput = typeof(StoryOnlineMenu).GetMethod("ResetChatInput", BindingFlags.Instance | BindingFlags.NonPublic);
                storyupdatelog = typeof(StoryOnlineMenu).GetMethod("UpdateLogDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
                storychattoggled = typeof(StoryOnlineMenu).GetField("isChatToggled", BindingFlags.Instance | BindingFlags.NonPublic);
                blockinput = typeof(ChatTextBox).GetField("blockInput", BindingFlags.NonPublic | BindingFlags.Static);
                new Hook(typeof(ChatTextBox).GetMethod("CaptureInputs", BindingFlags.Instance | BindingFlags.NonPublic), On_TextBox_CaptureInputs);
                //Logger.LogInfo("Capture Inputs hook applied!");
                new Hook(typeof(ChatTextBox).GetConstructor(new Type[] { typeof(Menu.Menu), typeof(MenuObject), typeof(string), typeof(Vector2), typeof(Vector2) }), On_TextBox_ctor);
                //Logger.LogInfo("Chat Constructor hook applied!");
                new Hook(typeof(ChatHud).GetConstructor(new Type[] { typeof(HUD.HUD), typeof(RoomCamera) }), On_ChatHud_ctor);
                //Logger.LogInfo("Hud Constructor hook applied!");
                //new Hook(typeof(ChatTextBox).GetMethod("GetKeyDown", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Func<KeyCode, bool>), typeof(KeyCode )}, null), On_ChatTextBox_GetKeyDown);
                //Logger.LogInfo("GetKeyDown hook applied!");
                new Hook(typeof(ChatTextBox).GetMethod("GetKey", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Func<KeyCode, bool>), typeof(KeyCode) }, null), On_ChatTextBox_GetKey);
                //Logger.LogInfo("GetKey hook applied!");
                new ILHook(typeof(ChatTemplate).GetMethod(nameof(ChatTemplate.Update)), IL_ChatTemplate_Update);
                //Logger.LogInfo("Update hook applied!");
                IL.RainWorldGame.Update += IL_RainWorldGame_Update;
                IL.Menu.SlugcatSelectMenu.Update += IL_StoryArenaMenu_Update;
                IL.Menu.MultiplayerMenu.Update += IL_StoryArenaMenu_Update;
                On.Menu.MenuObject.GrafUpdate += On_MenuObject_GrafUpdate;
                On.Menu.Menu.SelectNewObject += On_Menu_SelectNewObject;
            } catch(Exception e)
            {
                Logger.LogError("Failed to load! " + e);
            }
            Logger.LogInfo("Better Meadow Typing successfully loaded");
        }
    }

    private static void On_MenuObject_GrafUpdate(On.Menu.MenuObject.orig_GrafUpdate orig, MenuObject self, float timeStacker)
    {
        if (self is ChatTextBox chat)
        {
            var msg = ChatTextBox.lastSentMessage;
            var len = msg.Length;
            if (len > 0)
            {
                checkkeys = true;
                if (Input.GetKey(KeyCode.Backspace) && chatcursor > 0)
                {
                    if (backspaceheld == 0 || (backspaceheld > 30 && (backspaceheld % 2 == 0)))
                    {
                        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                        {
                            ChatTextBox.lastSentMessage = "";
                            chat.menuLabel.text = ChatTextBox.lastSentMessage;
                            chatcursor = 0;
                        }
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            //.LogInfo("CTRL + Backspace captured!");
                            if (chatcursor > 0)
                            {
                                self.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                                //Logger.LogInfo("Got past the length check Message: " + msg);
                                int pos = (chatcursor > 0) ? chatcursor - 1 : 0;
                                int /*space = 0;
                if (validchars.Contains(msg[pos])) for (int i = pos; chatcursor < i; i--)
                    {
                        if(!validchars.Contains(msg[i]))
                        {
                            space = msg.Substring(0, i).LastIndexOfAny(validchars) + 1;
                            break;
                        }
                    }
                else*/ space = msg.Substring(0, pos).LastIndexOf(' ') + 1;
                                //Logger.LogInfo("Space index is " + space);
                                ChatTextBox.lastSentMessage = msg.Remove(space, chatcursor - space);
                                chat.menuLabel.text = ChatTextBox.lastSentMessage;
                                if (space > msg.Length) space = msg.Length;
                                chatcursor = space;
                            }
                        }
                    }
                    backspaceheld++;
                }
                else
                {
                    backspaceheld = 0;
                    if (Input.GetKey(KeyCode.LeftArrow) && chatcursor > 0)
                    {
                        if (arrowheld == 0 || (arrowheld > 30 && (arrowheld % 2 == 0)))
                        {
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                chatcursor = msg.Substring(0, chatcursor - 1).LastIndexOf(' ') + 1;
                            }
                            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            {
                                chatcursor = 0;
                            }
                            else chatcursor--;
                            if (chatcursor < len)
                            {
                                //Logger.LogInfo((self as ChatTemplate)._cursor.height);
                                //Logger.LogInfo((self as ChatTemplate)._cursor.width);
                                (self as ChatTemplate)._cursor.element = Futile.atlasManager.GetElementWithName("pixel");
                                (self as ChatTemplate)._cursor.height = 13f;
                                float width = LabelTest.GetWidth((self as ChatTemplate).menuLabel.label.text.Substring(0, chatcursor), false);
                                (self as ChatTemplate)._cursorWidth = width;
                                (self as ChatTemplate).cursorWrap.sprite.x = width + 12f + (self.menu is StoryOnlineMenu ? (self as ChatTemplate).pos.x - 4f : 0f);
                            }
                        }
                        arrowheld++;
                        //instance.Logger.LogInfo(chatcursor);
                    }
                    else if (Input.GetKey(KeyCode.RightArrow) && chatcursor < len)
                    {
                        if (arrowheld == 0 || (arrowheld > 30 && (arrowheld % 2 == 0)))
                        {
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                int space = msg.Substring(chatcursor, len - chatcursor - 1).IndexOf(' ');
                                if (space < 0 || space >= len) chatcursor = len;
                                else chatcursor = space + chatcursor + 1;

                            }
                            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            {
                                chatcursor = len;
                            }
                            else chatcursor++;
                            if (chatcursor == len)
                            {
                                (self as ChatTemplate)._cursor.element = Futile.atlasManager.GetElementWithName("modInputCursor");
                                (self as ChatTemplate)._cursor.height = 6f;
                                float width = LabelTest.GetWidth((self as ChatTemplate).menuLabel.label.text, false);
                                (self as ChatTemplate)._cursorWidth = width;
                                (self as ChatTemplate).cursorWrap.sprite.x = width + 19f + (self.menu is StoryOnlineMenu ? (self as ChatTemplate).pos.x - 4f : 0f);
                            }
                        }
                        arrowheld++;
                        //instance.Logger.LogInfo(chatcursor);
                    }
                    else arrowheld = 0;
                }
                checkkeys = false;
            }
        }
        orig(self, timeStacker);
    }

    private static void DestroyChatTextBox()
    {
        ChatTextBox.lastSentMessage = "";
        /*if (OpenChat.menu != null && OpenChat.menu is StoryOnlineMenu menu)
        {
            ChatTextBox.OnShutDownRequest -= (Action)Delegate.CreateDelegate(typeof(Action), menu, storyresetinput);
        }
        else */if (ActiveChat != null && chatinputoverlay.GetValue(ActiveChat) != null)
        {
            var overlay = chatinputoverlay.GetValue(ActiveChat) as ChatInputOverlay;
            overlay.chat.DelayedUnload(0.1f);
            overlay.ShutDownProcess();
            chatinputoverlay.SetValue(ActiveChat, null);
            ChatTextBox.OnShutDownRequest -= ActiveChat.ShutDownChatInput;
        }
        OpenChat = null;
    }

    /*private static float SlightlyLeft(float width, ChatTemplate self)
    {
        if (!(self is ChatTextBox) || self.menuLabel.text == null || self.menuLabel.text.Length < 1 || chatcursor < 1) return false;
        string str = self.menuLabel.text.Substring(0, chatcursor);
        self._cursorWidth = LabelTest.GetWidth(str, false);
        self.cursorWrap.sprite.x = self._cursorWidth + 18f;
        return true;
    }*/
    private static string MoveCursorDisplay(string text, ChatTemplate self)
    {
        if (!(self is ChatTextBox) || text.Length < 1) return text;
        if (chatcursor < 1) return "";
        return text.Substring(0, chatcursor);

    }
    private static float SlightlyLeft(float width, ChatTemplate self)
    {
        if (!(self is ChatTextBox)) return width;
        int len = self.menuLabel.label.text.Length;
        return width - ((len > 0 && chatcursor < len) ? 8f : 1f) - (self.menu is StoryOnlineMenu ? 4f : self.pos.x);
    }

    private static void IL_ChatTemplate_Update(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        //c.Emit(OpCodes.Ldarg_0);
        //c.EmitDelegate(MoveCursor);
        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchCallOrCallvirt<FLabel>("get_text")
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(MoveCursorDisplay);
            if (c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdcR4(20),
                x => x.MatchAdd()
            ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SlightlyLeft);
            }
            //else Logger.LogError("Failed to load ChatTemplate IL hook!");
        }
        //else Logger.LogError("Failed to load ChatTemplate IL hook!");
    }

    /*private bool On_ChatTextBox_GetKeyDown(Func<Func<KeyCode, bool>, KeyCode, bool> orig, Func<KeyCode, bool> self, KeyCode code)
    {
        if (code == KeyCode.LeftArrow || code == KeyCode.RightArrow) return self(code);
        return orig(self, code);
    }*/

    private static bool On_ChatTextBox_GetKey(Func<Func<KeyCode, bool>, KeyCode, bool> orig, Func<KeyCode, bool> self, KeyCode code)
    {
        if (checkkeys /*|| code == KeyCode.LeftControl || code == KeyCode.LeftAlt || code == KeyCode.LeftArrow || code == KeyCode.RightArrow || code == KeyCode.RightControl || code == KeyCode.RightAlt || code == KeyCode.Backspace*/) return self(code);
        return orig(self, code);
    }

    private static void On_ChatHud_ctor(Action<ChatHud, HUD.HUD, RoomCamera> orig, ChatHud self, HUD.HUD hud, RoomCamera camera)
    {
        //Logger.LogInfo("Hud constructed!");
        ActiveChat = self;
        orig(self, hud, camera);
    }

    private static void On_TextBox_ctor(Action<ChatTextBox, Menu.Menu, MenuObject, string, Vector2, Vector2> orig, ChatTextBox self, Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size)
    {
        //Logger.LogInfo("Text box constructed!");
        OpenChat = self;
        chatcursor = 0;
        orig(self, menu, owner, displayText, pos, size);
    }

	private static void On_TextBox_CaptureInputs(Action<ChatTextBox, char> orig, ChatTextBox self, char input)
	{
        //Logger.LogInfo("Input captured: " + input + " " + input.ToString());
        if (input == '\u007F') return;
        string msg = ChatTextBox.lastSentMessage;
        checkkeys = true;
        /*if (input == '\u007F' || ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && input == '\b'))
        {
            //.LogInfo("CTRL + Backspace captured!");
            if (chatcursor > 0) {
                self.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                //Logger.LogInfo("Got past the length check Message: " + msg);
                int pos = (chatcursor > 0) ? chatcursor - 1: 0;
                int /*space = 0;
                if (validchars.Contains(msg[pos])) for (int i = pos; chatcursor < i; i--)
                    {
                        if(!validchars.Contains(msg[i]))
                        {
                            space = msg.Substring(0, i).LastIndexOfAny(validchars) + 1;
                            break;
                        }
                    }
                else space = msg.Substring(0, pos).LastIndexOf(' ') + 1;
                //Logger.LogInfo("Space index is " + space);
                ChatTextBox.lastSentMessage = msg.Remove(space, chatcursor - space);
                if (space > msg.Length) space = msg.Length;
                chatcursor = space;
            }
        }
        else*/ if (input == '\b' || input == '\u0008' && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            if (chatcursor > 0)
            {
                self.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                ChatTextBox.lastSentMessage = msg.Remove(chatcursor - 1, 1);
                chatcursor--;
            }
        }
        /*else if (input == char)
        {
            if (msg.Length > 0 && !string.IsNullOrWhiteSpace(msg))
            {
                MatchmakingManager.currentInstance.SendChatMessage(msg);
                foreach (var player in OnlineManager.players)
                {
                    player.InvokeRPC(RPCs.UpdateUsernameTemporarily, msg);
                }
            }
            else
            {
                self.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                RainMeadow.RainMeadow.Debug("Could not send lastSentMessage because it had no text or only had whitespaces");
            }
          (typeof(ChatTextBox).GetField("OnShutDownRequest", BindingFlags.Public | BindingFlags.Static).GetValue(null) as Action).Invoke();
            (typeof(ChatTextBox).GetField("typingHandler", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self) as ButtonTypingHandler).Unassign(self);
        }*/
        /*else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && input == 'v')
        {
            self.menu.PlaySound(SoundID.MENU_Checkbox_Check);
            ChatTextBox.lastSentMessage = (msg + GUIUtility.systemCopyBuffer).Substring(0, ChatTextBox.textLimit);
            chatcursor = ChatTextBox.lastSentMessage.Length - 1;
        }*/
        else if (input == '\n' || input == '\r')
        {
            if (msg.Length > 0 && !string.IsNullOrWhiteSpace(msg))
            {
                MatchmakingManager.currentInstance.SendChatMessage(msg);
                foreach (var player in OnlineManager.players)
                {
                    player.InvokeRPC(RPCs.UpdateUsernameTemporarily, msg);
                }
            }
            else
            {
                self.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                RainMeadow.RainMeadow.Debug("Could not send lastSentMessage because it had no text or only had whitespaces");
            }
            if (self.menu != null && self.menu is StoryOnlineMenu)
            {
                ChatTextBox.lastSentMessage = "";
                chatcursor = 0;
                OpenChat = null;
            }
            else DestroyChatTextBox();
            //else DestroyChatTextBox();
            //(shutdownrequest.GetValue(null) as Action).Invoke();
            //(typinghandler.GetValue(self) as ButtonTypingHandler).Unassign(self);
        }
        else
        {
            if (msg.Length < ChatTextBox.textLimit)
            {
                self.menu.PlaySound(SoundID.MENU_Checkbox_Check);
                ChatTextBox.lastSentMessage = msg.Insert(chatcursor, input.ToString());
                chatcursor++;
            }
        }
        checkkeys = false;
        self.menuLabel.text = ChatTextBox.lastSentMessage;
    }

    private static bool IsChatOpen()
    {
        //instance.Logger.LogInfo("IS CHAT OPEN CALLED " + typeof(ChatTextBox).GetField("blockInput", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
        if (OpenChat != null)
        {
            bool hasmenu = OpenChat.menu != null;
            if(hasmenu) OpenChat.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
            //ActiveChat.ShutDownChatInput();
            if (!ChatHud.isLogToggled && ActiveChat != null) ActiveChat.ShutDownChatLog();
            DestroyChatTextBox();
            //(typinghandler.GetValue(OpenChat) as ButtonTypingHandler).Unassign(OpenChat);
            return true;
        }
        return false;
    }

    private static void IL_RainWorldGame_Update(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel skip = null;
        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchCallOrCallvirt<RoomCamera>("get_roomSafeForPause"),
            x => x.MatchBrfalse(out skip)
            /* x => x.MatchLdarg(0),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld(out var _),
            x => x.MatchLdarg(0),
            x => x.MatchNewobj(out var _),
            x => x.MatchStfld<RainWorldGame>("pauseMenu")*/
            ))
        {
            //c.Index++;
            c.MoveAfterLabels();
            c.EmitDelegate(IsChatOpen);
            c.Emit(OpCodes.Brtrue_S, skip);
        }
        //else Logger.LogError("Failed to load RainWorldGame IL hook!");
    }

    /*private static bool CheckPauseStory(bool pause, SlugcatSelectMenu self)
    {
        //instance.Logger.LogInfo("IS CHAT OPEN CALLED " + typeof(ChatTextBox).GetField("blockInput", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
        if (pause && !self.lastPauseButton && OpenChat != null && (bool)blockinput.GetValue(null))
        {
            OpenChat.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
            //ActiveChat.ShutDownChatInput();
            if (!ChatHud.isLogToggled) ActiveChat.ShutDownChatLog();
            DestroyChatTextBox();
            //(typinghandler.GetValue(OpenChat) as ButtonTypingHandler).Unassign(OpenChat);
            return true;
        }
        return pause;
    }

    private static bool CheckPauseArena(bool pause, MultiplayerMenu self)
    {
        //instance.Logger.LogInfo("IS CHAT OPEN CALLED " + typeof(ChatTextBox).GetField("blockInput", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
        if (pause && !self.lastPauseButton && OpenChat != null && (bool)blockinput.GetValue(null))
        {
            OpenChat.menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
            //ActiveChat.ShutDownChatInput();
            if (!ChatHud.isLogToggled) ActiveChat.ShutDownChatLog();
            DestroyChatTextBox();
            //(typinghandler.GetValue(OpenChat) as ButtonTypingHandler).Unassign(OpenChat);
            return true;
        }
        return pause;
    }*/

    private static bool ChatEscape(Menu.Menu self)
    {
        //instance.Logger.LogInfo("IS CHAT OPEN CALLED " + typeof(ChatTextBox).GetField("blockInput", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
        if (OpenChat != null && ((bool)blockinput.GetValue(null)))
        {
            self.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
            //ActiveChat.ShutDownChatInput();
            //DestroyChatTextBox();
            storychattoggled.SetValue(self, false);
            storyresetinput.Invoke(self, null);
            storyupdatelog.Invoke(self, null);
            ChatTextBox.lastSentMessage = "";
            //(typinghandler.GetValue(OpenChat) as ButtonTypingHandler).Unassign(OpenChat);
            return true;
        }
        return false;
    }

    private static void IL_StoryArenaMenu_Update(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILLabel skip = null;
        if(c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdloc(0),
            x => x.MatchBrfalse(out skip),
            x => x.MatchLdarg(0),
            x => x.MatchLdfld(out var _),
            x => x.MatchBrtrue(out var _)
            ))
        {
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(ChatEscape);
            c.Emit(OpCodes.Brtrue_S, skip);
        }
    }

    private static void On_Menu_SelectNewObject(On.Menu.Menu.orig_SelectNewObject orig, Menu.Menu self, RWCustom.IntVector2 direction)
    {
        if ((bool)blockinput.GetValue(null)) return;
        orig(self, direction);
    }

}