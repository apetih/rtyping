using System;
using Dalamud.Plugin.Ipc;

namespace rtyping
{
    // Mirrors ChatTwo.Code.ChatType (ushort-backed) — only needed for IPC type matching,
    // values are never read by this plugin.
    internal enum ChatTwoChannelType : ushort { }

    internal sealed class ChatTwoIpc : IDisposable
    {
        private readonly Plugin Plugin;

        private ICallGateSubscriber<object?>? _available;
        private ICallGateSubscriber<(bool, bool, bool, bool, int, ChatTwoChannelType), object?>? _stateChanged;

        internal ChatTwoIpc(Plugin plugin)
        {
            Plugin = plugin;

            try
            {
                _available = Plugin.PluginInterface.GetIpcSubscriber<object?>("ChatTwo.Available");
                _available.Subscribe(OnChatTwoAvailable);
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning(ex, "ChatTwo IPC: failed to subscribe to ChatTwo.Available");
            }

            TrySubscribeStateChanged();
        }

        private void TrySubscribeStateChanged()
        {
            try
            {
                _stateChanged = Plugin.PluginInterface
                    .GetIpcSubscriber<(bool InputVisible, bool InputFocused, bool HasText, bool IsTyping, int TextLength, ChatTwoChannelType ChannelType), object?>(
                        "ChatTwo.ChatInputStateChanged");
                _stateChanged.Subscribe(OnStateChanged);

                // Immediately sync state in case ChatTwo is already loaded and the user is typing.
                var query = Plugin.PluginInterface
                    .GetIpcSubscriber<(bool, bool, bool, bool, int, ChatTwoChannelType)>("ChatTwo.GetChatInputState");
                var state = query.InvokeFunc();
                Plugin.TypingManager.IPCTyping = state.Item4;
            }
            catch
            {
                // ChatTwo is not installed or not yet loaded — safe to ignore.
            }
        }

        private void OnChatTwoAvailable() => TrySubscribeStateChanged();

        private void OnStateChanged((bool InputVisible, bool InputFocused, bool HasText, bool IsTyping, int TextLength, ChatTwoChannelType ChannelType) state)
        {
            Plugin.TypingManager.IPCTyping = state.IsTyping;
        }

        public void Dispose()
        {
            try { _available?.Unsubscribe(OnChatTwoAvailable); } catch { }
            try { _stateChanged?.Unsubscribe(OnStateChanged); } catch { }

            // Clear the override so vanilla detection takes back over cleanly.
            Plugin.TypingManager.IPCTyping = false;
        }
    }
}
