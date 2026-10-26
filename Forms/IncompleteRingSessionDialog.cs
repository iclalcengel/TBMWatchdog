using TBMWatchdog.Models;

namespace TBMWatchdog.Forms;

public enum IncompleteRingChoice { Continue, CancelSession, CloseForNow }

public sealed class IncompleteRingSessionDialog : Form
{
    public IncompleteRingSessionDialog(RingSession session)
    {
        Text = "Tamamlanmamış Ring Oturumu"; StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        ShowInTaskbar = false; ClientSize = new Size(500, 190); BackColor = Color.FromArgb(15, 23, 42); ForeColor = Color.White;
        Controls.Add(new Label { Location = new Point(20, 18), Size = new Size(460, 92), Font = new Font("Segoe UI", 10F),
            Text = $"Tamamlanmamış ring oturumu bulundu:\n\nMakine: {session.MachineId}\nRing: {session.RingNo}\nDurum: {session.CurrentState}" });
        var resume = Button("Devam Et", 20, Color.FromArgb(2, 132, 199));
        var cancel = Button("İptal Et", 180, Color.FromArgb(153, 27, 27));
        var close = Button("Şimdilik Kapat", 340, Color.FromArgb(51, 65, 85));
        resume.Click += (_, _) => Select(IncompleteRingChoice.Continue);
        cancel.Click += (_, _) => Select(IncompleteRingChoice.CancelSession);
        close.Click += (_, _) => Select(IncompleteRingChoice.CloseForNow);
        Controls.AddRange([resume, cancel, close]);
    }
    public IncompleteRingChoice Choice { get; private set; } = IncompleteRingChoice.CloseForNow;
    private void Select(IncompleteRingChoice choice) { Choice = choice; DialogResult = DialogResult.OK; Close(); }
    private static Button Button(string text, int x, Color color) => new() { Text = text, Location = new Point(x, 130),
        Size = new Size(140, 38), FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White,
        FlatAppearance = { BorderSize = 0 } };
}
