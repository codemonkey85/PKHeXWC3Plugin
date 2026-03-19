using static System.Buffers.Binary.BinaryPrimitives;
using PKHeX.Core;

namespace WC3Plugin;

public static class MysteryDataUtil
{
    #region WC3
    /*
     * File Size:       INT 0x58C,         JP 0x4E4
     * Offsets:
     * WonderCard:      INT 0     - 0x14F, JP 0     - 0x0A7
     * WonderCardExtra: INT 0x150 - 0x177, JP 0x0A8 - 0x0CF // only for card type 2 (link stats)
     * Trainer IDs:     INT 0x178 - 0x19F, JP 0x0D0 - 0x0F7 // only for card type 2 (link stats)
     * MysteryData:     INT 0x1A0 - 0x58B, JP 0x0F8 - 0x4E3
     */
    public static void ImportWC3(this SAV3 sav, byte[] data)
    {
        if (sav.LargeBlock is not ISaveBlock3LargeExpansion wonder)
            return;

        int CardSize = GetWC3CardSize(sav);
        int WC3ScriptOffset = GetWC3ScriptOffset(sav);

        Memory<byte> memory = new(data);
        WonderCard3 wc3 = new(memory[..CardSize]);
        wc3.FixChecksum();
        wonder.SetWonderCard(sav.Japanese, wc3.Data);

        if (wc3.Type == CARD_TYPE_LINK_STAT)
        {
            int wcExtraOffset = CardSize;
            WonderCard3Extra wc3Extra = new(memory[wcExtraOffset..(wcExtraOffset + WonderCard3Extra.SIZE)]);
            // wc3Extra.FixChecksum(); checksum is unused in the games
            wonder.SetWonderCardExtra(sav.Japanese, wc3Extra.Data);

            // TODO: Handle import of Trainer IDs
        }

        MysteryEvent3 me3 = new(memory[WC3ScriptOffset..]);
        me3.FixChecksum();
        ((ISaveBlock3Large)sav.LargeBlock).MysteryData = me3;
    }

    public static ReadOnlySpan<byte> ExportWC3(this SAV3 sav)
    {
        if (sav.LargeBlock is not ISaveBlock3LargeExpansion wonder)
            return [];

        Span<byte> data = new byte[GetWC3FileSize(sav)];
        wonder.GetWonderCard(sav.Japanese).Data.CopyTo(data[0..]);

        if (wonder.GetWonderCard(sav.Japanese).Type == CARD_TYPE_LINK_STAT)
        {
            wonder.GetWonderCardExtra(sav.Japanese).Data.CopyTo(data[GetWC3CardSize(sav)..]);

            // TODO: Handle export of Trainer IDs
        }

        ((ISaveBlock3Large)sav.LargeBlock).MysteryData.Data.CopyTo(data[GetWC3ScriptOffset(sav)..]);
        return data;
    }

    public static bool HasWC3(this SAV3 sav)
    {
        return sav.LargeBlock is ISaveBlock3LargeExpansion wonder && !IsEmpty(wonder.GetWonderCard(sav.Japanese).Data);
    }

    public static int GetWC3FileSize(this SAV3 sav) => GetWC3ScriptOffset(sav) + MysteryEvent3.SIZE;

    private static int GetWC3CardSize(this SAV3 sav) => sav.Japanese ? WonderCard3.SIZE_JAP : WonderCard3.SIZE;
    private static int GetWC3ScriptOffset(this SAV3 sav) => GetWC3CardSize(sav) + (WonderCard3Extra.SIZE * 2);
    private const int CARD_TYPE_LINK_STAT = 2;
    #endregion WC3

    #region ME3
    public static void ImportME3(this SAV3 sav, byte[] data)
    {
        Memory<byte> memory = new(data);
        Gen3MysteryData mystery;
        if (sav.LargeBlock is ISaveBlock3LargeExpansion wonder) // FRLGE
        {
            wonder.SetWonderCard(sav.Japanese, new WonderCard3(new byte[sav.Japanese ? WonderCard3.SIZE_JAP : WonderCard3.SIZE]).Data);

            mystery = new MysteryEvent3(memory[..MysteryEvent3.SIZE]);
            ((MysteryEvent3)mystery).FixChecksum();
        }
        else // RS
        {
            mystery = new MysteryEvent3RS(memory[..MysteryEvent3.SIZE]);
            ((MysteryEvent3RS)mystery).FixChecksum();
        }
        ((ISaveBlock3Large)sav.LargeBlock).MysteryData = mystery;

        if (sav.LargeBlock is ISaveBlock3LargeHoenn hoenn && data.Length == MysteryEvent3.SIZE + RecordMixing3Gift.SIZE)
        {
            RecordMixing3Gift rm3 = new(memory[MysteryEvent3.SIZE..]);
            rm3.FixChecksum();
            hoenn.RecordMixingGift = rm3;
        }
    }

    public static ReadOnlySpan<byte> ExportME3(this SAV3 sav)
    {
        return ((ISaveBlock3Large)sav.LargeBlock).MysteryData.Data;
    }

    public static bool HasME3(this SAV3 sav)
    {
        return sav is SAV3RS
            ? !IsEmpty(((ISaveBlock3Large)sav.LargeBlock).MysteryData.Data)
            : sav.LargeBlock is ISaveBlock3LargeExpansion wonder && !IsEmpty(((ISaveBlock3Large)sav.LargeBlock).MysteryData.Data) && IsEmpty(wonder.GetWonderCard(sav.Japanese).Data);
    }
    #endregion ME3

    #region WN3
    public static void ImportWN3(this SAV3 sav, byte[] data)
    {
        if (sav.LargeBlock is not ISaveBlock3LargeExpansion wonder)
            return;

        Memory<byte> memory = new(data);
        WonderNews3 wn3 = new(memory);
        wn3.FixChecksum();

        wonder.SetWonderNews(sav.Japanese, wn3.Data);
    }

    public static ReadOnlySpan<byte> ExportWN3(this SAV3 sav)
    {
        if (sav.LargeBlock is not ISaveBlock3LargeExpansion wonder)
            return [];

        Span<byte> data = new byte[GetWN3FileSize(sav)];
        wonder.GetWonderNews(sav.Japanese).Data.CopyTo(data[0..]);
        return data;
    }

    public static bool HasWN3(this SAV3 sav)
    {
        return sav.LargeBlock is ISaveBlock3LargeExpansion wonder && !IsEmpty(wonder.GetWonderNews(sav.Japanese).Data);
    }

    public static int GetWN3FileSize(this SAV3 sav) => sav.Japanese ? WonderNews3.SIZE_JAP : WonderNews3.SIZE;
    #endregion WN3

    #region ECT
    public static void ImportECT(this SAV3 sav, byte[] data)
    {
        FixECTChecksum(data).CopyTo(((ISaveBlock3Small)sav.SmallBlock).EReaderTrainer);
    }

    public static ReadOnlySpan<byte> ExportECT(this SAV3 sav)
    {
        return ((ISaveBlock3Small)sav.SmallBlock).EReaderTrainer;
    }

    public static bool HasECT(this SAV3 sav)
    {
        return !IsEmpty(((ISaveBlock3Small)sav.SmallBlock).EReaderTrainer);
    }

    public static int GetECTFileSize(this SAV3 _) => ECT_SIZE;

    private static Span<byte> FixECTChecksum(Span<byte> data)
    {
        WriteUInt32LittleEndian(data[(ECT_SIZE - 4)..], GetECTChecksum(data));
        return data;
    }

    private static uint GetECTChecksum(Span<byte> data)
    {
        uint chk = 0;

        for (int i = 0; i < ECT_SIZE - 4; i += 4)
        {
            chk += ReadUInt32LittleEndian(data[i..]);
        }

        return chk;
    }

    private const int ECT_SIZE = 188;
    #endregion ECT

    #region ECB
    public static void ImportECB(this SAV3 sav, byte[] data)
    {
        FixECBChecksum(data).CopyTo(((ISaveBlock3Large)sav.LargeBlock).EReaderBerry);
        sav.SetWork((sav.LargeBlock is ISaveBlock3LargeHoenn) ? VAR_ENIGMA_BERRY_AVAILABLE_RSE : VAR_ENIGMA_BERRY_AVAILABLE_FRLG, 1);
    }

    public static ReadOnlySpan<byte> ExportECB(this SAV3 sav)
    {
        return ((ISaveBlock3Large)sav.LargeBlock).EReaderBerry;
    }

    public static bool HasECB(this SAV3 sav)
    {
        return !IsEmpty(((ISaveBlock3Large)sav.LargeBlock).EReaderBerry);
    }

    public static int GetECBFileSize(this SAV3 sav) => sav is SAV3RS ? ECB_SIZE_RS : ECB_SIZE_FRLGE;

    private static ReadOnlySpan<byte> FixECBChecksum(Span<byte> data)
    {
        WriteUInt16LittleEndian(data[(data.Length - 4)..], GetECBChecksum(data));
        return data;
    }

    private static ushort GetECBChecksum(ReadOnlySpan<byte> data)
    {
        ushort chk = 0;

        if (data.Length == ECB_SIZE_RS)
        {
            for (int i = 0; i < ECB_SIZE_RS - 4; i++)
            {
                if (i < 0xC || i >= 0x14)
                    chk += (ushort)(data[i] & 0xFF);
            }
        }
        else
        {
            for (int i = 0; i < ECB_SIZE_FRLGE - 4; i++)
            {
                chk += (ushort)(data[i] & 0xFF);
            }
        }

        return chk;
    }

    private const int ECB_SIZE_RS = 1328;
    private const int ECB_SIZE_FRLGE = 52;
    private const int VAR_ENIGMA_BERRY_AVAILABLE_RSE = 0x2D;  // 0x402D
    private const int VAR_ENIGMA_BERRY_AVAILABLE_FRLG = 0x33; // 0x4033; unused but set by script command
    #endregion ECB

    #region RM3
    public static void SetRecordMixing(this SAV3 sav, ushort item, byte count)
    {
        if (sav.LargeBlock is not ISaveBlock3LargeHoenn hoenn)
            return;

        RecordMixing3Gift rm3 = new(new byte[RecordMixing3Gift.SIZE])
        {
            Item = item,
            Count = (byte)(item == 0 ? 0 : count)
        };
        rm3.FixChecksum();
        hoenn.RecordMixingGift = rm3;
    }

    public static bool IsValidForRecordMixing(this SAV3 sav, ushort item)
    {
        if (sav.LargeBlock is not ISaveBlock3LargeHoenn)
            return false;

        if (item is 0 or EONTICKET)
            return true;

        IItemStorage storage = sav is SAV3E ? new ItemStorage3E() : new ItemStorage3RS();
        return storage.GetItems(InventoryType.PCItems).Contains(item);
    }

    public static IReadOnlyList<ComboItem> GetRecordMixingItemDataSource(this SAV3 sav) =>
    [
        .. GameInfo.FilteredSources.Items,
        new(GameInfo.Strings.GetItemStrings(sav.Context, sav.Version)[EONTICKET], EONTICKET),
    ];
    private const ushort EONTICKET = 0x113;
    #endregion RM3

    #region Utility
    private static bool IsEmpty(ReadOnlySpan<byte> data) => data.IndexOfAnyExcept<byte>(0, 0xFF) == -1;
    #endregion Utility
}
