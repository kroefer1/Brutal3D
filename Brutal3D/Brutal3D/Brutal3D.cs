// Brutal3D
// Requires OpenTK 4.x and System.Drawing.Common
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

// Aliases to avoid collisions
using WinFormsApp = System.Windows.Forms.Application;
using WinFormsLabel = System.Windows.Forms.Label;
using WinFormsComboBox = System.Windows.Forms.ComboBox;
using WinFormsNumericUpDown = System.Windows.Forms.NumericUpDown;
using WinFormsButton = System.Windows.Forms.Button;
using WinFormsCheckBox = System.Windows.Forms.CheckBox;

namespace Brutal3D
{
    class SettingsForm : Form
    {
        // existing
        public float CubeSpeed { get; private set; } = 50f;
        public int MSAA { get; private set; } = 4;
        public bool StartClicked { get; private set; } = false;
        public Color CubeColor { get; private set; } = Color.Teal;
        public string CubeQuality { get; private set; } = "High";
        public Vector2i Resolution { get; private set; } = new Vector2i(1280, 720);
        public string ScreenMode { get; private set; } = "Windowed";

        // new
        public bool VSyncEnabled { get; private set; } = true;
        public int FramerateCap { get; private set; } = 0; // 0 = uncapped
        public bool Wireframe { get; private set; } = false;
        public string TexturePath { get; private set; } = null;
        public int CubeCount { get; private set; } = 1;
        public bool DebugOverlay { get; private set; } = false;

        readonly WinFormsComboBox qualityBox;
        readonly WinFormsComboBox msaaBox;
        readonly WinFormsNumericUpDown speedBox;
        readonly WinFormsComboBox resBox;
        readonly WinFormsComboBox screenBox;
        readonly WinFormsButton colorBtn;

        // new controls
        readonly WinFormsCheckBox vsyncCheck;
        readonly WinFormsNumericUpDown framerateBox;
        readonly WinFormsCheckBox wireframeCheck;
        readonly WinFormsButton textureBtn;
        readonly WinFormsNumericUpDown cubeCountBox;
        readonly WinFormsCheckBox debugCheck;
        readonly WinFormsButton resetBtn;

        public SettingsForm()
        {
            Text = "Brutal3D";
            Width = 420;
            Height = 660; // slightly taller for separators
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            int labelX = 12;
            int controlX = 170;
            int spacingY = 36;
            int currentY = 18;

            // Helper to add a label + control
            void AddRow(Control label, Control control)
            {
                label.Top = currentY;
                label.Left = labelX;
                control.Top = currentY;
                control.Left = controlX;
                Controls.Add(label);
                Controls.Add(control);
                currentY += spacingY;
            }

            // Helper to add a section with a thin horizontal line
            void AddSection(string title)
            {
                var sectionLabel = new Label()
                {
                    Text = title,
                    Top = currentY,
                    Left = labelX,
                    AutoSize = true,
                    Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
                    ForeColor = Color.DimGray
                };
                Controls.Add(sectionLabel);
                currentY += 20;

                // Thin horizontal line
                var separator = new Label()
                {
                    Top = currentY - 4,
                    Left = labelX,
                    Width = ClientSize.Width - 24,
                    Height = 1,
                    BorderStyle = BorderStyle.Fixed3D
                };
                Controls.Add(separator);

                currentY += 16; // spacing after line
            }

            // --- Section: Cube Settings ---
            AddSection("Cube Settings");

            speedBox = new WinFormsNumericUpDown() { Minimum = 1, Maximum = 1000, Value = 50, Width = 220 };
            speedBox.ValueChanged += (s, e) => CubeSpeed = (float)speedBox.Value;
            AddRow(new WinFormsLabel() { Text = "Cube Speed:" }, speedBox);

            qualityBox = new WinFormsComboBox() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            qualityBox.Items.AddRange(new object[] { "Low", "Medium", "High", "Ultra" });
            qualityBox.SelectedIndex = 2;
            qualityBox.SelectedIndexChanged += (s, e) => CubeQuality = qualityBox.SelectedItem?.ToString() ?? "High";
            AddRow(new WinFormsLabel() { Text = "Cube Quality:" }, qualityBox);

            colorBtn = new WinFormsButton() { Text = "Pick Color", Width = 220, BackColor = CubeColor };
            colorBtn.Click += (s, e) =>
            {
                using var dlg = new ColorDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    CubeColor = dlg.Color;
                    colorBtn.BackColor = dlg.Color;
                }
            };
            AddRow(new WinFormsLabel() { Text = "Cube Color:" }, colorBtn);

            cubeCountBox = new WinFormsNumericUpDown() { Minimum = 1, Maximum = 32767, Value = 1, Width = 220 };
            cubeCountBox.ValueChanged += (s, e) => CubeCount = (int)cubeCountBox.Value;
            AddRow(new WinFormsLabel() { Text = "Cube Count:" }, cubeCountBox);

            textureBtn = new WinFormsButton() { Text = "Load Texture...", Width = 220 };
            textureBtn.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog();
                ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tga|All files|*.*";
                ofd.Title = "Select Cube Texture (large images may crash GPU)";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    TexturePath = ofd.FileName;
                    textureBtn.Text = Path.GetFileName(TexturePath);
                }
            };
            AddRow(new WinFormsLabel() { Text = "Cube Texture:" }, textureBtn);

            // --- Section: Graphics Settings ---
            AddSection("Graphics Settings");

            msaaBox = new WinFormsComboBox() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            msaaBox.Items.AddRange(new object[] {
        "--- Good ---","0","2","4","8","16",
        "--- Heavy ---","32","64","128",
        "--- Extreme ---","256","512","1024","2048"
    });
            msaaBox.SelectedIndex = 2;
            msaaBox.SelectedIndexChanged += (s, e) =>
            {
                var sel = msaaBox.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(sel) || sel.StartsWith("---")) return;
                if (!int.TryParse(sel, out int v)) v = 4;
                MSAA = v;
            };
            msaaBox.DrawMode = DrawMode.OwnerDrawFixed;
            msaaBox.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                var itemText = msaaBox.Items[e.Index].ToString();
                e.DrawBackground();
                using var brush = itemText.StartsWith("---") ? new SolidBrush(Color.Gray) : new SolidBrush(e.ForeColor);
                var font = itemText.StartsWith("---") ? new Font(e.Font, FontStyle.Italic) : e.Font;
                e.Graphics.DrawString(itemText, font, brush, e.Bounds.Left, e.Bounds.Top);
                e.DrawFocusRectangle();
            };
            AddRow(new WinFormsLabel() { Text = "MSAA:" }, msaaBox);

            resBox = new WinFormsComboBox() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            resBox.Items.AddRange(new object[]{
        "--- 4:3 ---","800x600","1024x768","1152x864","1280x960","1400x1050","1600x1200",
        "--- 16:10 ---","1280x800","1440x900","1680x1050","1920x1200","2560x1600",
        "--- 16:9 ---","1280x720","1366x768","1600x900","1920x1080","2560x1440","3200x1800","3840x2160",
        "--- 21:9 ---","2560x1080","3440x1440","5120x2160"
    });
            resBox.SelectedIndex = 2;
            resBox.SelectedIndexChanged += (s, e) =>
            {
                var sel = resBox.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(sel) || sel.StartsWith("---")) return;
                var parts = sel.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                    Resolution = new Vector2i(w, h);
            };
            resBox.DrawMode = DrawMode.OwnerDrawFixed;
            resBox.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                var itemText = resBox.Items[e.Index].ToString();
                e.DrawBackground();
                using var brush = itemText.StartsWith("---") ? new SolidBrush(Color.Gray) : new SolidBrush(e.ForeColor);
                var font = itemText.StartsWith("---") ? new Font(e.Font, FontStyle.Italic) : e.Font;
                e.Graphics.DrawString(itemText, font, brush, e.Bounds.Left, e.Bounds.Top);
                e.DrawFocusRectangle();
            };
            AddRow(new WinFormsLabel() { Text = "Resolution:" }, resBox);

            screenBox = new WinFormsComboBox() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            screenBox.Items.AddRange(new object[] { "Windowed", "Borderless", "Fullscreen" });
            screenBox.SelectedIndex = 0;
            screenBox.SelectedIndexChanged += (s, e) => ScreenMode = screenBox.SelectedItem?.ToString() ?? "Windowed";
            AddRow(new WinFormsLabel() { Text = "Screen Mode:" }, screenBox);

            // --- Section: Misc Settings ---
            AddSection("Misc Settings");

            vsyncCheck = new WinFormsCheckBox() { Text = "VSync", Width = 220, Checked = true };
            vsyncCheck.CheckedChanged += (s, e) => VSyncEnabled = vsyncCheck.Checked;
            AddRow(new WinFormsLabel() { Text = "VSync:" }, vsyncCheck);

            framerateBox = new WinFormsNumericUpDown() { Minimum = 0, Maximum = 1000, Value = 0, Width = 220 };
            framerateBox.ValueChanged += (s, e) => FramerateCap = (int)framerateBox.Value;
            AddRow(new WinFormsLabel() { Text = "Framecap (0=uncapped):" }, framerateBox);

            wireframeCheck = new WinFormsCheckBox() { Text = "Wireframe Mode", Width = 220 };
            wireframeCheck.CheckedChanged += (s, e) => Wireframe = wireframeCheck.Checked;
            AddRow(new WinFormsLabel() { Text = "Wireframe:" }, wireframeCheck);

            debugCheck = new WinFormsCheckBox() { Text = "Debug Overlay", Width = 220 };
            debugCheck.CheckedChanged += (s, e) => DebugOverlay = debugCheck.Checked;
            AddRow(new WinFormsLabel() { Text = "Debug Overlay:" }, debugCheck);

            resetBtn = new WinFormsButton() { Text = "Reset to Defaults", Width = 220 };
            resetBtn.Click += (s, e) => ResetToDefaults();
            AddRow(new WinFormsLabel() { Text = "" }, resetBtn);

            // Start button
            var startBtn = new WinFormsButton() { Text = "Start", Width = 120, Top = currentY, Left = controlX + 100 };
            startBtn.Click += (s, e) => { StartClicked = true; Close(); };
            Controls.Add(startBtn);

            // Version label
            var versionLabel = new WinFormsLabel()
            {
                Text = "Brutal3D v0.2",
                Top = ClientSize.Height - 28,
                Left = 8,
                ForeColor = Color.Gray,
                Font = new Font(Font.FontFamily, 8, FontStyle.Italic),
                AutoSize = true
            };
            Controls.Add(versionLabel);
        }


        private void ResetToDefaults()
        {
            speedBox.Value = 50;
            msaaBox.SelectedIndex = 2;
            qualityBox.SelectedIndex = 2;
            CubeColor = Color.Teal;
            colorBtn.BackColor = CubeColor;
            resBox.SelectedIndex = 2;
            screenBox.SelectedIndex = 0;

            vsyncCheck.Checked = true;
            framerateBox.Value = 0;
            wireframeCheck.Checked = false;
            TexturePath = null;
            textureBtn.Text = "Load Texture...";
            cubeCountBox.Value = 1;
            debugCheck.Checked = false;
        }
    }

    class Brutal3D : GameWindow
    {
        private float _cubeAngle = 0f;
        private float _cubeSpeed;
        private Vector3 _cubeColor;
        private int _shaderProgram;
        private int _quadProgram; // for debug overlay quad
        private int _vao;
        private int _vertexCount;

        // new
        private bool _wireframe;
        private int _textureId = 0;
        private bool _useTexture = false;
        private int _cubeCount = 1;
        private bool _debugOverlay = false;

        // debug text rendering
        private int _debugVao = 0;
        private int _debugVbo = 0;
        private int _debugTex = 0;
        private int _debugTexW = 256;
        private int _debugTexH = 128;
        private Bitmap _debugBitmap;

        // performance stats
        private Stopwatch _fpsSw = Stopwatch.StartNew();
        private double _frameTimeMs = 0;
        private double _fps = 0;
        private double _avgFps = 0;
        private int _frameCounter = 0;
        private double _accumTime = 0;

        //framerate cap
        private int _framerateCap = 0;

        // replace inside Brutal3D constructor, remove obsolete lines
        public Brutal3D(GameWindowSettings gws, NativeWindowSettings nws, float cubeSpeed, Color cubeColor, int quality,
                        bool vsync, int framerateCap, bool wireframe, string texturePath, int cubeCount, bool debugOverlay)
            : base(gws, nws)
        {
            _cubeSpeed = cubeSpeed;
            _cubeColor = new Vector3(cubeColor.R / 255f, cubeColor.G / 255f, cubeColor.B / 255f);
            _wireframe = wireframe;
            _cubeCount = Math.Max(1, cubeCount);
            _debugOverlay = debugOverlay;
            _framerateCap = framerateCap;

            BuildCubeMesh(quality);

            // VSync
            VSync = vsync ? VSyncMode.On : VSyncMode.Off;

            // Texture
            if (!string.IsNullOrEmpty(texturePath) && File.Exists(texturePath))
            {
                TryLoadTextureFromFile(texturePath, out _textureId, out _useTexture);
            }

            // Framerate cap is handled in Program.Main via window.Run(framerateCap)
        }


        private void TryLoadTextureFromFile(string path, out int texId, out bool success)
        {
            texId = 0;
            success = false;
            try
            {
                using var bmp = new Bitmap(path);
                GL.GetInteger(GetPName.MaxTextureSize, out int maxTexSize);
                if (bmp.Width > maxTexSize || bmp.Height > maxTexSize)
                    Debug.WriteLine($"Warning: texture {bmp.Width}x{bmp.Height} exceeds GL_MAX_TEXTURE_SIZE {maxTexSize}");

                texId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texId);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                        ImageLockMode.ReadOnly,
                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              data.Width, data.Height, 0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                bmp.UnlockBits(data);

                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                success = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load texture: {ex.Message}");
                if (texId != 0) GL.DeleteTexture(texId);
                texId = 0;
                success = false;
            }
        }

        private void BuildCubeMesh(int subdivisions)
        {
            List<float> verts = new List<float>();

            var faceDirs = new[]
            {
                (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY),
                (-Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY),
                (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
                (Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
                (Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ),
                (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
            };

            for (int face = 0; face < faceDirs.Length; face++)
            {
                var (normal, axisX, axisY) = faceDirs[face];
                for (int y = 0; y < subdivisions; y++)
                {
                    for (int x = 0; x < subdivisions; x++)
                    {
                        float step = 1f / subdivisions;
                        float x0 = -0.5f + x * step;
                        float y0 = -0.5f + y * step;
                        float x1 = x0 + step;
                        float y1 = y0 + step;

                        Vector3 v00 = normal * 0.5f + axisX * x0 + axisY * y0;
                        Vector3 v10 = normal * 0.5f + axisX * x1 + axisY * y0;
                        Vector3 v11 = normal * 0.5f + axisX * x1 + axisY * y1;
                        Vector3 v01 = normal * 0.5f + axisX * x0 + axisY * y1;

                        verts.AddRange(new float[] { v00.X, v00.Y, v00.Z, v10.X, v10.Y, v10.Z, v11.X, v11.Y, v11.Z });
                        verts.AddRange(new float[] { v11.X, v11.Y, v11.Z, v01.X, v01.Y, v01.Z, v00.X, v00.Y, v00.Z });
                    }
                }
            }

            float[] vertices = verts.ToArray();
            _vertexCount = vertices.Length / 3;

            _vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // unbind
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Enable(EnableCap.DepthTest);

            // shaders
            string vsSrc = @"
                #version 330 core
                layout(location=0) in vec3 aPos;
                uniform mat4 model;
                uniform mat4 view;
                uniform mat4 projection;
                out vec3 fragPos;
                void main() {
                    fragPos = aPos;
                    gl_Position = projection * view * model * vec4(aPos,1.0);
                }";

            string fsSrc = @"
                #version 330 core
                out vec4 FragColor;
                uniform vec3 cubeColor;
                uniform sampler2D cubeTex;
                uniform bool useTex;
                // a cheap planar mapping using position for UVs
                vec2 simpleUV(vec3 pos) {
                    return pos.xy + 0.5;
                }
                void main() {
                    if(useTex) {
                        vec2 uv = simpleUV(normalize(gl_FragCoord.xyz));
                        // fallback: use fragment position for some variation
                        FragColor = texture(cubeTex, gl_FragCoord.xy / 1024.0);
                    } else {
                        FragColor = vec4(cubeColor,1.0);
                    }
                }";

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vsSrc);
            GL.CompileShader(vs);
            CheckShaderCompile(vs);

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fsSrc);
            GL.CompileShader(fs);
            CheckShaderCompile(fs);

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vs);
            GL.AttachShader(_shaderProgram, fs);
            GL.LinkProgram(_shaderProgram);
            GL.GetProgram(_shaderProgram, GetProgramParameterName.LinkStatus, out int linked);
            if (linked == 0)
            {
                var log = GL.GetProgramInfoLog(_shaderProgram);
                Debug.WriteLine($"Shader link error: {log}");
            }
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);

            // quad shader for debug overlay (textured)
            string quadVs = @"
                #version 330 core
                layout(location=0) in vec2 aPos;
                layout(location=1) in vec2 aTex;
                out vec2 vTex;
                uniform mat4 ortho;
                void main() {
                    vTex = aTex;
                    gl_Position = ortho * vec4(aPos.xy, 0.0, 1.0);
                }";
            string quadFs = @"
                #version 330 core
                in vec2 vTex;
                out vec4 FragColor;
                uniform sampler2D tex;
                void main() {
                    FragColor = texture(tex, vTex);
                }";
            int qvs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(qvs, quadVs);
            GL.CompileShader(qvs);
            CheckShaderCompile(qvs);
            int qfs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(qfs, quadFs);
            GL.CompileShader(qfs);
            CheckShaderCompile(qfs);
            _quadProgram = GL.CreateProgram();
            GL.AttachShader(_quadProgram, qvs);
            GL.AttachShader(_quadProgram, qfs);
            GL.LinkProgram(_quadProgram);
            GL.DeleteShader(qvs);
            GL.DeleteShader(qfs);

            // debug quad VAO
            _debugVao = GL.GenVertexArray();
            _debugVbo = GL.GenBuffer();
            GL.BindVertexArray(_debugVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _debugVbo);
            // positions (x,y) and texcoords (u,v), will buffer dynamic per create
            float[] quadData = {
                // x, y, u, v
                10, 10, 0f, 1f,
                266, 10, 1f, 1f,
                266, 138, 1f, 0f,
                10, 138, 0f, 0f
            };
            GL.BufferData(BufferTarget.ArrayBuffer, quadData.Length * sizeof(float), quadData, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.BindVertexArray(0);

            // debug text texture
            _debugTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _debugTex);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            CreateOrUpdateDebugBitmap("Starting...", 256, 128);
            UploadDebugBitmap();

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void CheckShaderCompile(int shader)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                var log = GL.GetShaderInfoLog(shader);
                Debug.WriteLine($"Shader compile error: {log}");
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            _cubeAngle += _cubeSpeed * (float)e.Time;

            // compute FPS
            _frameTimeMs = e.Time * 1000.0;
            _fps = 1.0 / e.Time;
            _frameCounter++;
            _accumTime += e.Time;
            if (_accumTime >= 0.5)
            {
                _avgFps = _frameCounter / _accumTime;
                _frameCounter = 0;
                _accumTime = 0;
            }

            // input
            if (KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
                Close();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            // Frame cap handling
            if (_framerateCap > 0)
            {
                double targetFrameTime = 1.0 / _framerateCap;
                double sleepTime = targetFrameTime - e.Time;
                if (sleepTime > 0)
                    System.Threading.Thread.Sleep((int)(sleepTime * 1000));
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // wireframe toggle
            GL.PolygonMode(MaterialFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);

            GL.UseProgram(_shaderProgram);
            GL.BindVertexArray(_vao);

            var projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                Size.X / (float)Size.Y,
                0.1f,
                100f
            );
            var view = Matrix4.CreateTranslation(0f, 0f, -6f);

            // Geometry layout for cubes
            int count = _cubeCount;
            int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
            int rows = (int)Math.Ceiling(count / (float)cols);
            float spacing = 1.6f;
            int idx = 0;

            // Bind texture if available
            if (_useTexture && _textureId != 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _textureId);
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "cubeTex"), 0);
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "useTex"), 1);
            }
            else
            {
                GL.Uniform1(GL.GetUniformLocation(_shaderProgram, "useTex"), 0);
                GL.Uniform3(GL.GetUniformLocation(_shaderProgram, "cubeColor"), _cubeColor);
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (idx >= count) break;

                    float x = (c - (cols - 1) * 0.5f) * spacing;
                    float y = ((rows - 1) * 0.5f - r) * spacing;
                    var model = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_cubeAngle + idx * 2f)) *
                                Matrix4.CreateTranslation(x, y, 0f);

                    GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "model"), false, ref model);
                    GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "view"), false, ref view);
                    GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram, "projection"), false, ref projection);

                    GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
                    idx++;
                }
            }

            // restore polygon mode
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            // debug overlay
            if (_debugOverlay)
                UpdateDebugTextureAndRenderOverlay();

            SwapBuffers();
        }

        private void UpdateDebugTextureAndRenderOverlay()
        {
            string vendor = GL.GetString(StringName.Vendor) ?? "Unknown";
            string renderer = GL.GetString(StringName.Renderer) ?? "Unknown";
            string version = GL.GetString(StringName.Version) ?? "Unknown";
            string debugText =
                $"FPS: {_fps:F1} (avg {_avgFps:F1})\n" +
                $"Frame: {_frameTimeMs:F2} ms\n" +
                $"Resolution: {Size.X}x{Size.Y}\n" +
                $"Cubes: {_cubeCount}\nVSync: {VSync}\nWireframe: {_wireframe}\n" +
                $"GL Vendor: {vendor}\nRenderer: {renderer}\nVersion: {version}";

            int padding = 8;
            int w = 300; // fixed width
            int h = padding * 2 + 16 * debugText.Split('\n').Length; // dynamic height

            CreateOrUpdateDebugBitmap(debugText, w, h);
            UploadDebugBitmap();

            GL.UseProgram(_quadProgram);
            var ortho = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1f, 1f);
            GL.UniformMatrix4(GL.GetUniformLocation(_quadProgram, "ortho"), false, ref ortho);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _debugTex);
            GL.Uniform1(GL.GetUniformLocation(_quadProgram, "tex"), 0);

            float margin = 10f;
            float x0 = margin;
            float y1 = Size.Y - margin;         // top-left corner
            float x1 = x0 + _debugTexW;
            float y0 = y1 - _debugTexH;        // go down by texture height

            float[] quadData = {
    x0, y0, 0f, 1f,   // bottom-left of quad -> top of texture
    x1, y0, 1f, 1f,   // bottom-right -> top
    x1, y1, 1f, 0f,   // top-right -> bottom
    x0, y1, 0f, 0f    // top-left -> bottom
};

            GL.BindBuffer(BufferTarget.ArrayBuffer, _debugVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, quadData.Length * sizeof(float), quadData);
            GL.BindVertexArray(_debugVao);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }


        private void CreateOrUpdateDebugBitmap(string text, int w, int h)
        {
            // Dispose previous if size changed
            if (_debugBitmap == null || _debugBitmap.Width != w || _debugBitmap.Height != h)
            {
                _debugBitmap?.Dispose();
                _debugBitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                _debugTexW = w;
                _debugTexH = h;
            }
            using (Graphics g = Graphics.FromImage(_debugBitmap))
            {
                g.Clear(Color.FromArgb(160, 0, 0, 0)); // semi transparent background
                using var font = new Font("Consolas", 10);
                using var brush = new SolidBrush(Color.Lime);
                var rect = new RectangleF(8, 6, w - 16, h - 12);
                g.DrawString(text, font, brush, rect);
            }
        }

        private void UploadDebugBitmap()
        {
            if (_debugBitmap == null) return;

            var data = _debugBitmap.LockBits(
                new Rectangle(0, 0, _debugBitmap.Width, _debugBitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb // fully qualified
            );

            GL.BindTexture(TextureTarget.Texture2D, _debugTex);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                _debugBitmap.Width,
                _debugBitmap.Height,
                0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, // fully qualified OpenTK PixelFormat
                PixelType.UnsignedByte,
                data.Scan0
            );

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            _debugBitmap.UnlockBits(data);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }


        protected override void OnUnload()
        {
            base.OnUnload();
            if (_shaderProgram != 0) GL.DeleteProgram(_shaderProgram);
            if (_quadProgram != 0) GL.DeleteProgram(_quadProgram);
            if (_vao != 0) GL.DeleteVertexArray(_vao);
            if (_debugVao != 0) GL.DeleteVertexArray(_debugVao);
            if (_debugVbo != 0) GL.DeleteBuffer(_debugVbo);
            if (_textureId != 0) GL.DeleteTexture(_textureId);
            if (_debugTex != 0) GL.DeleteTexture(_debugTex);
            _debugBitmap?.Dispose();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            WinFormsApp.EnableVisualStyles();
            WinFormsApp.SetCompatibleTextRenderingDefault(false);

            using var form = new SettingsForm();
            WinFormsApp.Run(form);
            if (!form.StartClicked) return;

            var gws = GameWindowSettings.Default;
            var nws = new NativeWindowSettings()
            {
                ClientSize = form.Resolution,
                Title = "Brutal3D",
                NumberOfSamples = form.MSAA,
                StartFocused = true,
                StartVisible = true,
                WindowBorder = form.ScreenMode switch
                {
                    "Windowed" => WindowBorder.Fixed,   // <- FIX: no resize
                    "Borderless" => WindowBorder.Hidden,
                    _ => WindowBorder.Resizable          // just in case
                }
            };

            if (form.ScreenMode == "Fullscreen")
                nws.WindowState = WindowState.Fullscreen;

            int quality = form.CubeQuality switch
            {
                "Low" => 1,
                "Medium" => 4,
                "High" => 10,
                "Ultra" => 20,
                _ => 1
            };

            using var window = new Brutal3D(gws, nws, form.CubeSpeed, form.CubeColor, quality,
                                           form.VSyncEnabled, form.FramerateCap, form.Wireframe, form.TexturePath, form.CubeCount, form.DebugOverlay);
            window.Run();

        }
    }
}

