using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;


namespace Core
{
	public class DataGridViewProgressColumn : DataGridViewColumn
	{
		public override DataGridViewCell CellTemplate
		{
			get
			{
				return base.CellTemplate;
			}
			set
			{
				if (value != null && !value.GetType().IsAssignableFrom(typeof(DataGridViewProgressCell)))
				{
					throw new InvalidCastException("Must be a DataGridViewProgressCell");
				}
				base.CellTemplate = value;
			}
		}

		public DataGridViewProgressColumn() : base(new DataGridViewProgressCell())
		{
		}
	}

	public class DataGridViewProgressCell : DataGridViewTextBoxCell
	{
		private Brush brushPercent;

		public override Type EditType
		{
			get
			{
				return null;
			}
		}

		public DataGridViewProgressCell()
		{
			this.Style.Format = @"0\%";
			this.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;			
			this.Style.ForeColor = Color.FromKnownColor(KnownColor.Black); // www.flounder.com/csharp_color_table.htm
			this.Style.SelectionForeColor = Color.FromKnownColor(KnownColor.Black);
		}

		protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			if(value == null)
				value = 0;

			double scale = Math.Min(100, Math.Max(Convert.ToDouble(value), 0)) / 100;

			base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, 
				DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.ErrorIcon | DataGridViewPaintParts.Focus | 
				DataGridViewPaintParts.SelectionBackground);

			if (scale > 0.0)
			{
				Rectangle rect = new Rectangle
				{
					X = cellBounds.X + 4,
					Y = cellBounds.Y + 2,
					Width = (int)Math.Round((cellBounds.Width - 10) * scale),
					Height = cellBounds.Height - 6
				};
				if (rect.Width > 0)
				{
					if (this.brushPercent != null)
					{
						this.brushPercent.Dispose();
						this.brushPercent = null;
					}
					//this.brushPercent = new LinearGradientBrush(rect, Color.White, Color.DarkGray, LinearGradientMode.Vertical);
					//this.brushPercent = new LinearGradientBrush(rect, Color.Khaki, Color.Khaki, LinearGradientMode.Vertical);
					this.brushPercent = new LinearGradientBrush(rect, Color.Khaki, Color.DarkGray, LinearGradientMode.Vertical);
					graphics.FillRectangle(this.brushPercent, rect);
					graphics.DrawRectangle(Pens.DimGray, rect);
				}
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText, cellStyle, advancedBorderStyle,
					DataGridViewPaintParts.ContentForeground);
			}
		}
	}

}
