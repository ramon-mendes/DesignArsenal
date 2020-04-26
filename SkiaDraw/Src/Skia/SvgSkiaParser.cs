using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using SkiaSharp;

namespace SkiaDraw
{
	public class SvgSkiaParser
	{
		public SvgScaler _scaler = new SvgScaler();
		public SKPath _path = new SKPath();

		private List<float> _operands;
		private string _svg;
		private int _ic = 0;
		private char _last_cmd;
		private float _posx;
		private float _posy;
		private float _last_control_x;
		private float _last_control_y;

		public static SvgSkiaParser FromPath(string svgpath)
		{
			var skp = new SvgSkiaParser();
			skp.Parse(svgpath);
			return skp;
		}

		private void Parse(string svgpath)
		{
			_svg = svgpath;
			_path.MoveTo(0, 0);

			while(_ic != _svg.Length)
			{
				char cmd = _svg[_ic++];
				ReadOperands();

				switch(cmd)
				{
					case 'M': case 'm': AppendMoveTo(cmd=='m'); break;
					case 'L': case 'l': AppendLineTo(cmd == 'l'); break;
					case 'H': case 'h': AppendHLineTo(cmd == 'h'); break;
					case 'V': case 'v': AppendVLineTo(cmd == 'v'); break;

					case 'C': case 'c': AppendCubicCurve(cmd == 'c'); break;
					case 'S': case 's': AppendShorthandCubicCurve(cmd == 's'); break;

					case 'Q': case 'q': AppendQuadraticCurve(cmd == 'q'); break;
					case 'T': case 't': AppendShorthandQuadraticCurve(cmd == 't'); break;

					case 'A': case 'a': AppendArc(cmd == 'a'); break;
						
					case 'Z': case 'z':
						_scaler.AddOperator('Z');
						_path.Close();
						break;
				}

				_last_cmd = cmd;
			}
		}

		private void ReadOperands()
		{
			_operands = new List<float>();
			while(true)
			{
				if(_ic == _svg.Length)
					return;
				
				StringBuilder sb = new StringBuilder();
				char c = _svg[_ic];
				if(char.IsLetter(c))
					return;

				if(c == '-')
				{
					sb.Append(c);
					_ic++;
				}
				else if(c == ' ' || c == '\t' || c == ',')// , is a number separator
				{
					_ic++;
					continue;
				}

				while(true)
				{
					c = _svg[_ic];
					if(char.IsNumber(c) || c=='.')
					{
						sb.Append(c);
						if(++_ic == _svg.Length)
							break;
						continue;
					}
					break;
				}

				string f = sb.ToString();
				_operands.Add(float.Parse(f, CultureInfo.InvariantCulture));
			}
		}

		private void AppendMoveTo(bool relative)
		{
			if(_operands.Count % 2 != 0)
				throw new Exception("Invalid parameter count in M style token");

			for(int i = 0; i < _operands.Count; i+=2)
			{
				float x = _operands[i];
				float y = _operands[i+1];

				if(relative)
				{
					x += _posx;
					y += _posy;
				}
				_path.MoveTo(x, y);

				_posx = x;
				_posy = y;
				_scaler.AddXYOperands(x, y);
			}
			_scaler.AddOperator('M');
		}

		private void AppendLineTo(bool relative)
		{
			if(_operands.Count % 2 != 0)
				throw new Exception("Invalid parameter count in L style token");

			for(int i = 0; i < _operands.Count; i += 2)
			{
				float x = _operands[i];
				float y = _operands[i + 1];

				if(relative)
				{
					x += _posx;
					y += _posy;
				}
				_path.LineTo(x, y);

				_posx = x;
				_posy = y;
				_scaler.AddXYOperands(x, y);
			}
			_scaler.AddOperator('L');
		}

		private void AppendHLineTo(bool relative)
		{
			for(int i = 0; i < _operands.Count; i++)
			{
				float x = _operands[i];
				float y = _posy;

				if(relative)
				{
					x += _posx;
				}
				_path.LineTo(x, y);

				_posx = x;
				_scaler.AddXOperand(x);
			}
			_scaler.AddOperator('H');
		}

		private void AppendVLineTo(bool relative)
		{
			for(int i = 0; i < _operands.Count; i++)
			{
				float x = _posx;
				float y = _operands[i];

				if(relative)
				{
					y += _posy;
				}
				_path.LineTo(x, y);

				_posy = y;
				_scaler.AddYOperand(y);
			}
			_scaler.AddOperator('V');
		}

		private void AppendCubicCurve(bool relative)
		{
			if(_operands.Count % 6 != 0)
				throw new Exception("Invalid number of parameters for C command");

			for(int i = 0; i < _operands.Count; i+=6)
			{
				float x1 = _operands[i + 0];
				float y1 = _operands[i + 1];
				float x2 = _operands[i + 2];
				float y2 = _operands[i + 3];
				float x = _operands[i + 4];
				float y = _operands[i + 5];

				if(relative)
				{
					x1 += _posx;
					y1 += _posy;
					x2 += _posx;
					y2 += _posy;
					x += _posx;
					y += _posy;
				}
				_path.CubicTo(x1, y1, x2, y2, x, y);

				_last_control_x = x2;
				_last_control_y = y2;

				_posx = x;
				_posy = y;

				_scaler.AddXYOperands(x1, y1);
				_scaler.AddXYOperands(x2, y2);
				_scaler.AddXYOperands(x, y);
			}
			_scaler.AddOperator('C');
		}

		private void AppendShorthandCubicCurve(bool relative)
		{
			if(_operands.Count % 4 != 0)
				throw new Exception("Invalid number of parameters for S command");
			if(_last_cmd != 'C' && _last_cmd != 'c' && _last_cmd != 'S' &&_last_cmd != 's')
			{
				_last_control_x = _posx;
				_last_control_y = _posy;
			}

			for(int i = 0; i < _operands.Count; i += 4)
			{
				float x1 = _posx + (_posx - _last_control_x);
				float y1 = _posy + (_posy - _last_control_y);
				float x2 = _operands[i + 0];
				float y2 = _operands[i + 1];
				float x = _operands[i + 2];
				float y = _operands[i + 3];

				if(relative)
				{
					x2 += _posx;
					y2 += _posy;
					x += _posx;
					y += _posy;
				}

				_path.CubicTo(x1, y1, x2, y2, x, y);

				_last_control_x = x2;
				_last_control_y = y2;

				_posx = x;
				_posy = y;

				_scaler.AddXYOperands(x2, y2);
				_scaler.AddXYOperands(x, y);
			}
			_scaler.AddOperator('S');
		}

		private void AppendQuadraticCurve(bool relative)
		{
			if(_operands.Count % 4 != 0)
				throw new Exception("Invalid number of parameters for Q command");

			for(int i = 0; i < _operands.Count; i += 4)
			{
				float x1 = _operands[i + 0];
				float y1 = _operands[i + 1];
				float x = _operands[i + 2];
				float y = _operands[i + 3];

				if(relative)
				{
					var posx = _posx;
					var posy = _posy;
					x1 += posx;
					y1 += posy;
					x += posx;
					y += posy;
				}
				_path.QuadTo(x1, y1, x, y);

				_last_control_x = x1;
				_last_control_y = y1;

				_posx = x;
				_posy = y;

				_scaler.AddXYOperands(x1, y1);
				_scaler.AddXYOperands(x, y);
			}
			_scaler.AddOperator('Q');
		}

		private void AppendShorthandQuadraticCurve(bool relative)
		{
			if(_operands.Count % 2 != 0)
				throw new Exception("Invalid number of parameters for Q command");

			if(_last_cmd != 'Q' && _last_cmd != 'q' && _last_cmd != 'T' && _last_cmd != 't')
			{
				_last_control_x = _posx;
				_last_control_y = _posy;
			}

			for(int i = 0; i < _operands.Count; i += 2)
			{
				var posx = _posx;
				var posy = _posy;

				float x1 = posx + (posx - _last_control_x);
				float y1 = posy + (posy - _last_control_y);
				float x = _operands[i + 0];
				float y = _operands[i + 1];

				if(relative)
				{
					x += posx;
					y += posy;
				}

				_path.QuadTo(x1, y1, x, y);

				_last_control_x = x1;
				_last_control_y = y1;

				_posx = x;
				_posy = y;

				_scaler.AddXYOperands(x, y);
			}
			_scaler.AddOperator('T');
		}

		private void AppendArc(bool relative)
		{
			if(_operands.Count % 7 != 0)
				throw new Exception("Invalid number of parameters for A command");
			
			Debug.Assert(false);
			for(int i = 0; i < _operands.Count; i += 7)
			{
				float x = _operands[i + 0];
				float y = _operands[i + 1];
				float angle = _operands[i + 2];

				//_cgpath.AddArc()
				//_path.ArcTo();
			}
		}
	}

	public class SvgScaler
	{
		private List<Tuple<char, int>> _operators = new List<Tuple<char, int>>();// operator, n of operators
		private List<float> _operands = new List<float>();
		//private PInvokeUtils.RECT _bounds = new PInvokeUtils.RECT();
		private int _cnt_operands = 0;

		public void Scale(float factor)
		{
			_operands = _operands.Select(f => f * factor).ToList();
		}

		public void AddOperator(char c)
		{
			_operators.Add(Tuple.Create(c, _cnt_operands));
			_cnt_operands = 0;
		}

		public void AddXYOperands(float x, float y)
		{
			AddXOperand(x);
			AddYOperand(y);
		}
		public void AddXOperand(float x)
		{
			/*if(x < _bounds.left)
				_bounds.left = (int)x;
			else if(x > _bounds.right)
				_bounds.right = (int)x;*/
			_cnt_operands++;
			_operands.Add(x);
		}
		public void AddYOperand(float y)
		{
			/*if(y < _bounds.bottom)
				_bounds.bottom = (int)y;
			else if(y > _bounds.top)
				_bounds.right = (int)y;*/
			_cnt_operands++;
			_operands.Add(y);
		}

		public string ToPath()
		{
			StringBuilder sb = new StringBuilder();
			int opcnt = 0;

			foreach(var item in _operators)
			{
				sb.Append(item.Item1);
				for(int i = 0; i < item.Item2; i++)
				{
					float f = _operands[opcnt++];
					sb.Append(' ');
					sb.Append(f.ToString(CultureInfo.InvariantCulture));
				}
			}
			Debug.Assert(opcnt == _operands.Count);
			return sb.ToString();
		}
	}
}