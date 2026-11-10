namespace GamepadRumbleBridge;

partial class PadSettingsForm
{
    private System.ComponentModel.IContainer components = null;
    private FlowLayoutPanel flowLayoutPanel;
    private Button btnApply;
    private Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.flowLayoutPanel = new FlowLayoutPanel();
        this.btnApply = new Button();
        this.btnCancel = new Button();
        this.SuspendLayout();
        // 
        // flowLayoutPanel
        // 
        this.flowLayoutPanel.AutoSize = true;
        this.flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
        this.flowLayoutPanel.Location = new Point(12, 12);
        this.flowLayoutPanel.Name = "flowLayoutPanel";
        this.flowLayoutPanel.Size = new Size(340, 280);
        this.flowLayoutPanel.TabIndex = 0;
        // 
        // btnApply
        // 
        this.btnApply.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
        this.btnApply.DialogResult = DialogResult.OK;
        this.btnApply.Location = new Point(197, 303);
        this.btnApply.Name = "btnApply";
        this.btnApply.Size = new Size(75, 30);
        this.btnApply.TabIndex = 1;
        this.btnApply.Text = "Применить";
        this.btnApply.UseVisualStyleBackColor = true;
        this.btnApply.Click += new EventHandler(btnApply_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
        this.btnCancel.DialogResult = DialogResult.Cancel;
        this.btnCancel.Location = new Point(278, 303);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new Size(75, 30);
        this.btnCancel.TabIndex = 2;
        this.btnCancel.Text = "Отмена";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new EventHandler(btnCancel_Click);
        // 
        // PadSettingsForm
        // 
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(364, 348);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnApply);
        this.Controls.Add(this.flowLayoutPanel);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "PadSettingsForm";
        this.ShowIcon = false;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Настройки геймпадов";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
