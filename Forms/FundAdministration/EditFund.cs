﻿using Range = Microsoft.Office.Interop.Excel.Range;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Watchdog.Entities;
using Watchdog.Forms.Util;
using Watchdog.Persistence;

namespace Watchdog.Forms.FundAdministration
{
    public partial class EditFund : Form
    {
        private TableLayoutPanel tableLayoutPanelAA;
        private readonly Fund fund;
        private readonly IPassedForm passedForm;
        private double totalRow;
        private double totalCol;

        public EditFund(IPassedForm passedForm, Fund fund)
        {
            InitializeComponent();
            this.fund = fund;
            this.passedForm = passedForm;
            InitializeCustomComponents();
            LoadFundProperties();
            LoadAssetAllocationTable();
            CalcTotals();
        }

        private void InitializeCustomComponents()
        {
            tableLayoutPanelAA = FormUtility.CreateTableLayoutPanel(1000, 200);
            FormUtility.AddValidation(buttonSubmit, textBoxCurrency, () =>
            {
                TableUtility tableUtility = new TableUtility();
                List<Range> currencyRange = tableUtility.ReadTableRow(Currency.GetDefaultValue(), new Dictionary<string, string>
                {
                    {"IsoCode", textBoxCurrency.Text.ToUpper() }
                }, QueryOperator.OR);

                if (currencyRange.Count == 0 || currencyRange.Count > 1)
                {
                    textBoxCurrency.BackColor = Color.Red;
                    return false;
                }
                if (MergeFundProperties() && MergeAssetAllocation())
                {
                    passedForm.OnSubmit();
                    Close();
                }
                return true;
            });
            FormUtility.AddControlsToForm(this, tableLayoutPanelAA);
        }

        private void LoadFundProperties()
        {
            textBoxFundName.Text = fund.Name;
            textBoxIsin.Text = fund.Isin;
            textBoxCustodyNr.Text = fund.CustodyAccountNumber;
            textBoxCurrency.Text = fund.Currency.IsoCode;
        }

        private Label GenerateLabel(string text, Persistable bindingObject)
        {
            Padding padding = Padding.Empty;
            Padding margin = new Padding(1, 0, 0, 1);
            Label label =  new Label
            {
                Text = text,
                Width = 150,
                Height = 50,
                Margin = margin,
                Padding = padding,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(Font, Font.Style | FontStyle.Bold)
            };
            FormUtility.BindObjectToControl(label, bindingObject);
            return label;
        }

        private double GetColumnSum(int col)
        {
            double sum = 0;
            for (int row = 1; row < tableLayoutPanelAA.RowCount - 1; row++)
            {
                FlowLayoutPanel panel = tableLayoutPanelAA.GetControlFromPosition(col, row) as FlowLayoutPanel;
                double.TryParse(panel.Controls[1].Text, out double number);
                sum += number;
            }
            return sum;
        }

        private double GetRowSum(int row)
        {
            double sum = 0;
            for (int col = 1; col < tableLayoutPanelAA.ColumnCount - 1; col++)
            {
                FlowLayoutPanel panel = tableLayoutPanelAA.GetControlFromPosition(col, row) as FlowLayoutPanel;
                double.TryParse(panel.Controls[1].Text, out double number);
                sum += number;
            }
            return sum;
        }

        private double GetTotalSum()
        {
            double sum = 0;
            for (int row = 1; row < tableLayoutPanelAA.RowCount - 1; row++)
            {
                int col = tableLayoutPanelAA.ColumnCount - 1;
                Label rowSum = tableLayoutPanelAA.GetControlFromPosition(col, row) as Label;
                double.TryParse(rowSum.Text, out double number);
                sum += number;
            }
            return sum;
        }

        private AssetAllocationEntry GetAssetAllocationEntry(AssetClass assetClass, Currency currency)
        {
            TableUtility tableUtility = new TableUtility();
            List<AssetAllocationEntry> entries = tableUtility.ConvertRangesToObjects<AssetAllocationEntry>(tableUtility.ReadAllRows(AssetAllocationEntry.GetDefaultValue()));
            var entryQuery = from entry in entries
                             where entry.AssetClass.Name.Equals(assetClass.Name)
                             where entry.Currency.IsoCode.Equals(currency.IsoCode)
                             where entry.Fund.Index == fund.Index
                             select entry;

            List<AssetAllocationEntry> list = entryQuery.ToList();
            if (list.Count == 0)
            {
                return null;
            }
            return entryQuery.ToList()[0];
        }

        private void LoadAssetAllocationTable()
        {
            TableUtility tableUtility = new TableUtility();
            List<AssetClass> assetClasses = tableUtility.ConvertRangesToObjects<AssetClass>(tableUtility.ReadAllRows(AssetClass.GetDefaultValue()));
            List<Currency> currencies = tableUtility.ConvertRangesToObjects<Currency>(tableUtility.ReadAllRows(Currency.GetDefaultValue()));
            int numberOfColumns = assetClasses.Count + 1;
            int numberOfRows = currencies.Count + 1;
            tableLayoutPanelAA.ColumnCount = numberOfColumns + 1;
            tableLayoutPanelAA.RowCount = numberOfRows + 1;

            // First cell, should be empty
            tableLayoutPanelAA.Controls.Add(GenerateLabel(string.Empty, null), 0, 0);

            Padding padding = Padding.Empty;

            // Add column labels for asset classes
            for (int col = 1; col < numberOfColumns; col++)
            {
                AssetClass assetClass = assetClasses[col - 1];
                Label columnLabel = GenerateLabel(assetClass.Name, assetClass);
                tableLayoutPanelAA.Controls.Add(columnLabel, col, 0);
            }

            // Add total column
            tableLayoutPanelAA.Controls.Add(GenerateLabel("Total", null), numberOfColumns, 0);

            // Add row labels for currencies
            for (int row = 1; row < numberOfRows; row++)
            {
                Currency currency = currencies[row - 1];
                Label rowLabel = GenerateLabel(currency.IsoCode, currency);
                tableLayoutPanelAA.Controls.Add(rowLabel, 0, row);
            }

            // Add total row
            tableLayoutPanelAA.Controls.Add(GenerateLabel("Total", null), 0, numberOfRows);

            // Add text boxes
            for (int row = 1; row < numberOfRows; row++)
            {
                for (int col = 1; col < numberOfColumns; col++)
                {
                    AssetClass assetClass = tableLayoutPanelAA.GetControlFromPosition(col, 0).DataBindings[0].DataSource as AssetClass;
                    Currency currency = tableLayoutPanelAA.GetControlFromPosition(0, row).DataBindings[0].DataSource as Currency;
                    Padding paddingOneLeft = new Padding(1, 0, 0, 0);
                    FlowLayoutPanel panel = new FlowLayoutPanel
                    {
                        Margin = paddingOneLeft,
                        Height = 50,
                        Width = 150,
                    };

                    for (int i = 0; i < 3; i++)
                    {
                        TextBox textBox = new TextBox
                        {
                            AutoSize = false,
                            Width = 50,
                            Height = 50,
                            Margin = padding,
                            BorderStyle = BorderStyle.FixedSingle,
                            TextAlign = HorizontalAlignment.Right
                        };
                        textBox.KeyUp += (tb, keyUp) =>
                        {
                            TextBox t = tb as TextBox;
                            bool cellContentIsNumber = double.TryParse(t.Text, out _);
                            if (!cellContentIsNumber)
                            {
                                t.Clear();
                            }
                        };
                        panel.Controls.Add(textBox);
                    }
                    panel.Controls[1].KeyUp += (tb, keyUp) =>
                    {
                        TextBox textBox = tb as TextBox;
                        FlowLayoutPanel flowPanel = ((TextBox)tb).Parent as FlowLayoutPanel;
                        CalcTotals(flowPanel);
                    };

                    AssetAllocationEntry entry = GetAssetAllocationEntry(assetClass, currency);
                    if (entry != null)
                    {
                        panel.Controls[0].Text = entry.StrategicMinValue.ToString();
                        panel.Controls[1].Text = entry.StrategicOptValue.ToString();
                        panel.Controls[2].Text = entry.StrategicMaxValue.ToString();
                        FormUtility.BindObjectToControl(panel, entry);
                    }


                    tableLayoutPanelAA.Controls.Add(panel);
                }
            }

            // Add total labels in last column
            for (int row = 1; row < numberOfRows + 1; row++)
            {
                tableLayoutPanelAA.Controls.Add(GenerateLabel("", null), numberOfColumns, row);
            }

            // Add total labels in last row
            for (int col = 1; col < numberOfColumns; col++)
            {
                tableLayoutPanelAA.Controls.Add(GenerateLabel("", null), col, numberOfRows);
            }
        }

        private void CalcTotals(object tb)
        {
            int numberOfRows = tableLayoutPanelAA.RowCount - 1;
            int numberOfColumns = tableLayoutPanelAA.ColumnCount - 1;
            TableLayoutPanelCellPosition position = tableLayoutPanelAA.GetPositionFromControl(tb as FlowLayoutPanel);
            Label totalAssetClass = tableLayoutPanelAA.GetControlFromPosition(position.Column, numberOfRows) as Label;
            Label totalCurrency = tableLayoutPanelAA.GetControlFromPosition(numberOfColumns, position.Row) as Label;
            Label total = tableLayoutPanelAA.GetControlFromPosition(numberOfColumns, numberOfRows) as Label;
            double rowSum = GetRowSum(position.Row);
            double columnSum = GetColumnSum(position.Column);
            totalCurrency.Text = rowSum.ToString();
            totalAssetClass.Text = columnSum.ToString();
            double totalSum = GetTotalSum();
            total.Text = totalSum.ToString();
        }

        private void CalcTotals()
        {
            totalCol = 0;
            totalRow = 0;
            for (int col = 1; col < tableLayoutPanelAA.ColumnCount - 1; col++)
            {
                double colSum = GetColumnSum(col);
                totalCol += colSum;
                Label label = tableLayoutPanelAA.GetControlFromPosition(col, tableLayoutPanelAA.RowCount - 1) as Label;
                label.Text = colSum.ToString();
            }
            for (int row = 1; row < tableLayoutPanelAA.RowCount - 1; row++)
            {
                double rowSum = GetRowSum(row);
                totalRow += rowSum;
                Label label = tableLayoutPanelAA.GetControlFromPosition(tableLayoutPanelAA.ColumnCount - 1, row) as Label;
                label.Text = rowSum.ToString();
            }
            tableLayoutPanelAA.GetControlFromPosition(tableLayoutPanelAA.ColumnCount - 1, tableLayoutPanelAA.RowCount - 1).Text = totalCol.ToString();
        }

        private void CancelButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private bool MergeAssetAllocation()
        {
            TableUtility tableUtility = new TableUtility();
            tableUtility.CreateTable(AssetAllocationEntry.GetDefaultValue());
            CalcTotals();
            if (totalRow != 100 || totalCol != 100)
            {
                MessageBox.Show("Gesamttotal muss 100% sein.");
                return false;
            }
            for (int col = 1; col < tableLayoutPanelAA.ColumnCount - 1; col++)
            {
                for (int row = 1; row < tableLayoutPanelAA.RowCount - 1; row++)
                {
                    FlowLayoutPanel panel = tableLayoutPanelAA.GetControlFromPosition(col, row) as FlowLayoutPanel;
                    double[] assetAllocationRange = new double[3];

                    for (int i = 0; i < 3; i++)
                    {
                        bool parsingSuccessful = double.TryParse(panel.Controls[i].Text, out double value);
                        if (!parsingSuccessful)
                        {
                            value = 0;
                        }
                        assetAllocationRange[i] = value;
                    }

                    try
                    {
                        AssetAllocationEntry entry = panel.DataBindings[0].DataSource as AssetAllocationEntry;
                        tableUtility.MergeTableRow(entry);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        AssetAllocationEntry assetAllocationEntry = new AssetAllocationEntry
                        {
                            AssetClass = tableLayoutPanelAA.GetControlFromPosition(col, 0).DataBindings[0].DataSource as AssetClass,
                            Currency = tableLayoutPanelAA.GetControlFromPosition(0, row).DataBindings[0].DataSource as Currency,
                            StrategicMinValue = assetAllocationRange[0],
                            StrategicOptValue = assetAllocationRange[1],
                            StrategicMaxValue = assetAllocationRange[2],
                            Fund = fund
                        };
                        tableUtility.InsertTableRow(assetAllocationEntry);
                    }
                }
            }
            return true;
        }

        private bool MergeFundProperties()
        {
            TableUtility tableUtility = new TableUtility();
            Fund newFund = new Fund
            {
                Index = fund.Index,
                Name = textBoxFundName.Text,
                CustodyAccountNumber = textBoxCustodyNr.Text,
                Isin = textBoxIsin.Text
            };
            List<Range> currencyRange = tableUtility.ReadTableRow(Currency.GetDefaultValue(), new Dictionary<string, string>
            {
                {"IsoCode", textBoxCurrency.Text.ToUpper() }
            }, QueryOperator.OR);

            if (currencyRange.Count != 0)
            {
                Currency currency = tableUtility.ConvertRangesToObjects<Currency>(currencyRange)[0];
                newFund.Currency = currency;
            }
            else
            {
                newFund.Currency = fund.Currency;
            }

            if (!fund.Equals(newFund))
            {
                tableUtility.MergeTableRow(newFund);
                return true;
            }
            return true;
        }
    }
}
