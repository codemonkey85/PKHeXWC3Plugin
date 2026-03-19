using PKHeX.Core;

namespace WC3Plugin;

public partial class RM3Form : Form
{
    private readonly SAV3 sav;

    public RM3Form(SAV3 sav)
    {
        this.sav = sav;

        InitializeComponent();

        ItemComboBox.DataSource = new BindingSource(sav.GetRecordMixingItemDataSource(), string.Empty);
        ItemComboBox.DisplayMember = nameof(ComboItem.Text);
        ItemComboBox.ValueMember = nameof(ComboItem.Value);

        ItemComboBox.SelectedValue = sav.IsValidForRecordMixing(((ISaveBlock3LargeHoenn)sav.LargeBlock).RecordMixingGift.Item) ? ((ISaveBlock3LargeHoenn)sav.LargeBlock).RecordMixingGift.Item : 0;
        CountBox.Value = ((ISaveBlock3LargeHoenn)sav.LargeBlock).RecordMixingGift.Count;
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
        sav.SetRecordMixing((ushort)((ComboItem)ItemComboBox.SelectedItem!).Value, (byte)CountBox.Value);
        sav.State.Edited = true;
        Close();
    }

    private void ItemComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        ushort selected = (ushort)((ComboItem)ItemComboBox.SelectedItem!).Value;
        if (selected == 0)
        {
            CountBox.Value = 0;
        }
        else if (selected != 0 && CountBox.Value == 0)
        {
            CountBox.Value = 1;
        }
    }
}
