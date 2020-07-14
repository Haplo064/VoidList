using System;
using Dalamud.Plugin;
using ImGuiNET;
using Dalamud.Configuration;
using Num = System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Command;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VoidList
{
    public class VoidList : IDalamudPlugin
    {
        public string Name => "VoidList";
        private DalamudPluginInterface pluginInterface;
        public Config Configuration;

        public bool enabled = true;
        public bool config = false;
        public string reason = "Reason for VoidListing";
        public byte[] buff = new byte[128];

        public int TargetID = 0;

        public List<Void> voidList = new List<Void>();

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();
            voidList = Configuration.VoidList;
            enabled = Configuration.Enabled;


            this.pluginInterface.CommandManager.AddHandler("/void", new CommandInfo(Command)
            {
                HelpMessage = "Shows the config for the VoidList."
            });

            this.pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            this.pluginInterface.Framework.Gui.Chat.OnChatMessage += Chat_OnChatMessage;
        }

        public void Dispose()
        {
            this.pluginInterface.UiBuilder.OnBuildUi -= DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi -= ConfigWindow;
            this.pluginInterface.CommandManager.RemoveHandler("/void");
            pluginInterface.Framework.Gui.Chat.OnChatMessage -= Chat_OnChatMessage;
        }

        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (enabled)
            {
                try
                {
                    if (!isHandled)
                    {

                        for (int i = 0; i < voidList.Count(); i++)
                        {
                            if (voidList[i].Name == sender.TextValue)
                            {
                                isHandled = true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    //lol
                }
            }
        }

                    public void Command(string command, string arguments)
        {
            config = true;
        }

        private void ConfigWindow(object Sender, EventArgs args)
        {
            config = true;
        }

        private void DrawWindow()
        {
            if (config)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(300, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("VoidList Config");

                float footer = (ImGui.GetStyle().ItemSpacing.Y) / 2 + ImGui.GetFrameHeightWithSpacing();
                ImGui.BeginChild("scrolling", new Num.Vector2(0, -footer), false);

                ImGui.Checkbox("Enable", ref enabled);

                ImGui.Columns(4);
                ImGui.Text("Who"); ImGui.NextColumn();
                ImGui.Text("When"); ImGui.NextColumn();
                ImGui.Text("Why"); ImGui.NextColumn();
                ImGui.NextColumn();

                int delete = -1;
                for(int i = 0; i < voidList.Count(); i++)
                {

                    ImGui.Text(voidList[i].Name); ImGui.NextColumn();
                    ImGui.Text(voidList[i].Time.ToString()); ImGui.NextColumn();
                    ImGui.Text(voidList[i].Reason); ImGui.NextColumn();
                    if (ImGui.Button("Remove##"+i.ToString()))
                        {
                        delete = i;
                        }
                    ImGui.NextColumn();

                }
                if (delete != -1)
                {
                    voidList.RemoveAt(delete);
                }

                ImGui.Columns(1);
                ImGui.Separator();

                if (ImGui.Button("Refresh"))
                {
                    for (var k = 0; k < this.pluginInterface.ClientState.Actors.Length; k++)
                    {
                        RerenderActor(this.pluginInterface.ClientState.Actors[k]);
                    }

                }

                if (pluginInterface.ClientState.LocalPlayer != null)
                {
                    if (pluginInterface.ClientState.LocalPlayer.TargetActorID != 0)
                    {
                        TargetID = pluginInterface.ClientState.LocalPlayer.TargetActorID;
                    }
                    else
                    {
                        TargetID = 0;
                    }


                    Dalamud.Game.ClientState.Actors.ActorTable actorTable = pluginInterface.ClientState.Actors;

                    for (var k = 0; k < this.pluginInterface.ClientState.Actors.Length; k++)
                    {
                        var actor = this.pluginInterface.ClientState.Actors[k];

                        if (actor == null)
                            continue;

                        if (actor.ActorId == TargetID && actor is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter)
                        {
                            ImGui.Text("Name: " + actor.Name);
                            ImGui.InputText(reason, buff, 128);
                            if (ImGui.Button("Void Player"))
                            {
                                voidList.Add(new Void(actor, System.Text.Encoding.Default.GetString(buff)));
                                buff = new byte[128];
                            }
                        }

                    }
                }
                ImGui.EndChild();

                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();

                    config = false;
                }
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Changes will only be saved for the current session unless you do this!"); }

                ImGui.End();

            }

            if (enabled && !pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.BoundByDuty])
            {
                for (var k = 0; k < this.pluginInterface.ClientState.Actors.Length; k++)
                {
                    var actor = this.pluginInterface.ClientState.Actors[k];

                    if (actor == null)
                        continue;

                    if (actor is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc)
                    {
                        foreach (Void person in voidList)
                        {
                            if (person.ActorId == actor.ActorId)
                            {
                                HideActor(actor);
                            }
                        }
                    }
                }
            }
        }

        public void SaveConfig()
        {
            Configuration.Enabled = enabled;
            Configuration.VoidList = voidList;
            this.pluginInterface.SavePluginConfig(Configuration);
        }

        private async void RerenderActor(Dalamud.Game.ClientState.Actors.Types.Actor a)
        {
            await Task.Run(async () => {
                try
                {
                    var addrEntityType = a.Address + 0x8C;
                    var addrRenderToggle = a.Address + 0x104;
                    if (a is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter)
                    {
                        Marshal.WriteByte(addrEntityType, 2);
                        Marshal.WriteInt32(addrRenderToggle, 2050);
                        await Task.Delay(100);
                        Marshal.WriteInt32(addrRenderToggle, 0);
                        await Task.Delay(100);
                        Marshal.WriteByte(addrEntityType, 1);
                    }
                    else
                    {
                        Marshal.WriteInt32(addrRenderToggle, 2050);
                        await Task.Delay(10);
                        Marshal.WriteInt32(addrRenderToggle, 0);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex.ToString());
                }
            });
        }

        private async void HideActor(Dalamud.Game.ClientState.Actors.Types.Actor a)
        {
            await Task.Run(async () => {
                try
                {
                    var addrEntityType = a.Address + 0x8C;
                    var addrRenderToggle = a.Address + 0x104;



                    if (a is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter)
                    {
                        Marshal.WriteByte(addrEntityType, 2);
                        Marshal.WriteInt32(addrRenderToggle, 2050);
                        //await Task.Delay(100);
                        //Marshal.WriteInt32(addrRenderToggle, 0);
                        await Task.Delay(100);
                        Marshal.WriteByte(addrEntityType, 1);
                    }

                    else
                    {
                        Marshal.WriteInt32(addrRenderToggle, 2050);
                        //await Task.Delay(10);
                        //Marshal.WriteInt32(addrRenderToggle, 0);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.LogError(ex.ToString());
                }
            });
        }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public List<Void> VoidList { get; set; } = new List<Void>();

    }

    public class Void
    {
        public string Name { get; set; } = "Apple Sauce";
        public DateTime Time { get; set; } = DateTime.Now;
        public int ActorId { get; set; } = 0;
        public string Reason { get; set; } = "Fuck'em";

        public Void(Dalamud.Game.ClientState.Actors.Types.Actor actor, string reason)
        {
            Name = actor.Name;
            Time = DateTime.Now;
            ActorId = actor.ActorId;
            Reason = reason;
        }

        [JsonConstructor] public Void(string name, DateTime time, int actorId, string reason)
        {
            Name = name;
            Time = time;
            ActorId = actorId;
            Reason = reason;
        }
    }
}
