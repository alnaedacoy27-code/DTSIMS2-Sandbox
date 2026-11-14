DTSIMS2 SANDBOX  -  experimental copy, isolated from the original

What is different from the original project
 1. Database default:   dtsims2_sandbox   (original: dtsims2_db)
 2. Settings folder:    %APPDATA%\DTSIMS2-Sandbox\   (original: %APPDATA%\DTSIMS2\)
      -> connection.cfg and devices.cfg are separate, so the two apps never overwrite each other
 3. Output exe name:    DTSIMS2-Sandbox.exe
 4. New project GUID / solution file: DTSIMS2-Sandbox.slnx
 Everything else (namespace DTSIMS2, all forms/modules) is unchanged so any file that works
 here can be copied straight back into the original project.

First-time setup
 1. Extract to its own folder, e.g. C:\DTSIMS2-Sandbox\
 2. MySQL Workbench: File > Open SQL Script > setup_sandbox_full.sql > run all (schema + data + v6 upgrade)
    Already imported the old dump? Just run upgrade_sandbox_to_v6.sql instead.
 3. Open DTSIMS2-Sandbox.slnx in Visual Studio, restore NuGet (MySql.Data), Rebuild, F5.
 4. The status bar (bottom of the main window) should read  "... | dtsims2_sandbox".
    If it says dtsims2_db, stop - you are pointed at the real database.

Moving a finished experiment back to the original
 - Copy the changed .vb files into the original project.
 - If you changed the database, save the changes as a .sql script and run it on dtsims2_db.
   Keep Db.RequiredSchemaVersion in sync with the schema_version table.

EXPERIMENT 1 - display (bigger, darker text; fuller screens)
 - Modules/Theme.vb: FontScale (1.12), BodyFamily, MakeFont(); text colors a bit darker.
   Every screen now builds its fonts through Theme.MakeFont, so ONE number resizes all text.
   For bolder body text set BodyFamily = "Segoe UI Semibold".
 - Controls/HomeView.vb + TileButton.vb: home tiles stretch to fill the row (max 4 per row, max 400 wide).
 - Controls/PosControls.vb (ProductTile) + Forms/Process/PosForm.vb: POS tiles are bigger and fill the width.
 - Controls/UiKit.vb (StatCard): slightly larger number cards.
 Files NOT touched on purpose: receipt, barcode labels, loyalty card drawing (print sizes must stay exact).

EXPERIMENT 1b - Text Size setting
 - Settings > Text Size: Default (current look), Medium, Large. Saved per computer (devices.cfg), applied on restart.
 - Theme.MakeFont: display numbers of 24 pt and up keep their size (fixes the huge VAT number); other text is capped at 24 pt.
 - MainMenuForm: the content area gets scroll bars when the window is smaller than the larger text needs.
 - VatForm: help text no longer cut off; the page scrolls.
 - New files: Modules/TextSize.vb, Forms/Settings/TextSizeForm.vb (both added to DTSIMS2.vbproj).

EXPERIMENT 1c - containers follow the text size
 - Theme.LayoutScale (1.00 Default / 1.125 Medium / 1.25 Large) and Theme.S(px): scales a pixel size.
 - About 730 layout sizes (positions, heights, widths, paddings, paint offsets, grid rows, layout constants)
   in Forms/ and the layout controls now go through Theme.S(). At Default nothing changes.
 - Not scaled on purpose: MainMenuForm window size, receipts, barcode labels, loyalty card drawing.
 - A copy of the files before this step is not kept in the zip; compare with the previous zip if needed.

EXPERIMENT 2 - speed (slow main window, Customers freeze)
 Causes found:
  1. SmsService.LoadPortSetting scanned EVERY COM port (open + sleeps, ~1-2 s per port, Bluetooth ports can hang)
     whenever the saved modem port was missing - which is every time on a PC without the modem.
     It ran on the screen thread, twice at startup (StartQueueWorker + HasModem) and again on every
     Customers screen load/refresh (SmsService.StatusText).
  2. SessionTracker.LocalIp used Dns.GetHostEntry(hostname) on the screen thread right after login.
  3. "localhost" can be tried on IPv6 first before falling back.
 Fixes: the scan runs only in a background task at startup and from the Devices screen's own check
  (at most every 10 min unless forced); HasModem/StatusText never scan; LocalIp reads the adapters;
  localhost connects as 127.0.0.1.
 Timing log: %APPDATA%\DTSIMS2-Sandbox\perf.log (Modules/PerfLog.vb). Delete the PerfLog.Mark lines when done.

EXPERIMENT 3 - reports (Sales, Inventory, Delivery)
 New screens (Reports menu): Sales Report, Inventory Report, Delivery Report (new tile). Loss Report is left as the placeholder.
 Every report: Daily / Weekly / Monthly tabs + date arrows + date picker, status figures, charts beside the table,
 Save PDF, Save Excel (.xlsx), Print (preview, landscape). Data loads on a background thread (no freezing).
  Sales Report     : Summary (total sales, transactions, items, average, profit, discounts, top 10 products, sales per hour/day,
                     comparison with the previous period) | Transactions (search, double-click = receipt copy + reprint) | Categories
                     Cashiers see only their own sales.
  Inventory Report : Summary (received, sold, lost, adjusted, stock in vs out chart, top 10 moved out) | Movements log
  Delivery Report  : Summary (deliveries, units, cost, expected profit, cost by supplier, per-day cost) | Deliveries (double-click = items)
 Receipt copy: Forms/Reports/ReceiptCopyDialog.vb - uses the same renderer as the POS; the printout is stamped
  "*** REPRINT / COPY ***" (ReceiptModel.IsReprint) and every reprint is written to the audit trail.
 New files: Modules/ReportModel.vb, ReportCanvas.vb (one layout -> printer or PDF), ReportExport.vb (PDF, xlsx, print),
  ReportData.vb (all queries); Controls/BarChart.vb, PillTabs.vb; Forms/Reports/ReportFormBase.vb, SalesReportForm.vb,
  InventoryReportForm.vb, DeliveryReportForm.vb, ReceiptCopyDialog.vb. vbproj: added System.IO.Compression references.
 Notes: PDF / print / Excel write money as "PHP" (the PDF built-in font has no peso sign). Excel holds the figures and table (no charts).

EXPERIMENT 3b - report printing + date button
 - Print (reports): asks which printer to use first (the receipt printer is never preselected); only A4 / Letter / Legal paper
   is accepted - a receipt roll printer is refused with a message. Landscape. Then the print preview; its Print button prints.
 - Date: arrows and date box replaced by Controls/DateChip.vb (calendar icon + period text, opens a month calendar) and a Today button.
 - Sales Report > Transactions: "View receipt" (preview) and "Print copy" (one click, receipt printer, stamped REPRINT / COPY, audited).

EXPERIMENT 3c - fixes
 - Delivery Report > Deliveries: SQL alias "lines" is a reserved word in MySQL 8 -> renamed to item_count.
 - Sales Report: tab renamed "Receipts"; extra header button "Receipt copy" jumps to it; on-screen tip under the table.
