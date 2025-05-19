using System.Windows.Forms;

namespace EasySaveV2._0.Notifications
{
    public interface INotifier
    {
        void Info(string message);
        void Warn(string message);
    }

    public class MessageBoxNotifier : INotifier
    {
        public void Info(string message)
            => MessageBox.Show(message, "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

        public void Warn(string message)
            => MessageBox.Show(message, "Attention",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
