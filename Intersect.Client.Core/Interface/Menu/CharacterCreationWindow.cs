using System.IO;
using Intersect.Client.Core;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Core;
using Intersect.Framework.Core.GameObjects.PlayerClass;
using Intersect.Framework.Reflection;
using Intersect.GameObjects;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Intersect.Client.Interface.Menu;

public partial class CharacterCreationWindow : Window
{
    private readonly MainMenu _mainMenu;
    private readonly SelectCharacterWindow _selectCharacterWindow;

    private readonly IFont? _defaultFont;

    private readonly Panel _previewPanel;
    private readonly Panel _previewContainer;
    private readonly Panel _propertiesPanel;
    private readonly Panel _buttonsPanel;

    private readonly TextBox _nameInput;

    private readonly LabeledComboBox _classCombobox;
    private readonly LabeledComboBox _hairCombobox;
    private readonly Panel _genderInputPanel;
    private readonly LabeledCheckBox _genderMaleCheckbox;
    private readonly LabeledCheckBox _genderFemaleCheckbox;

    private readonly ImagePanel _preview;
    private ImagePanel[]? _renderLayers;
    private readonly Button _nextSpriteButton;
    private readonly Button _prevSpriteButton;
    private readonly Button _nextHairButton;
    private readonly Button _prevHairButton;

    // Buttons
    private readonly Button _createButton;

    private int _displaySpriteIndex = -1;
    private int _selectedHairIndex = -1;
    private readonly List<string> _availableHairs = [];
    private readonly List<KeyValuePair<int, ClassSprite>> _femaleSprites = [];
    private readonly List<KeyValuePair<int, ClassSprite>> _maleSprites = [];
    private Button _backButton;


    public CharacterCreationWindow(Canvas parent, MainMenu mainMenu, SelectCharacterWindow selectCharacterWindow) :
        base(parent, title: Strings.CharacterCreation.Title, modal: false, name: nameof(CharacterCreationWindow))
    {
        _mainMenu = mainMenu;
        _selectCharacterWindow = selectCharacterWindow;

        _defaultFont = GameContentManager.Current.GetFont(name: "sourcesansproblack");

        Alignment = [Alignments.Center];
        MinimumSize = new Point(x: 560, y: 240);
        IsClosable = false;
        IsResizable = false;
        InnerPanelPadding = new Padding(8);
        Titlebar.MouseInputEnabled = false;
        TitleLabel.FontSize = 14;
        TitleLabel.TextColorOverride = Color.White;

        _buttonsPanel = new Panel(this, name: nameof(_buttonsPanel))
        {
            Dock = Pos.Bottom,
            Margin = new Margin(0, 8, 0, 0),
            ShouldDrawBackground = false,
        };

        _createButton = new Button(_buttonsPanel, name: nameof(_createButton))
        {
            Alignment = [Alignments.Left],
            Font = _defaultFont,
            FontSize = 12,
            MinimumSize = new Point(120, 24),
            Text = Strings.CharacterCreation.Create,
        };
        _createButton.Clicked += CreateButton_Clicked;

        _backButton = new Button(_buttonsPanel, name: nameof(_backButton))
        {
            Alignment = [Alignments.Right],
            Font = _defaultFont,
            FontSize = 12,
            MinimumSize = new Point(120, 24),
            Text = Strings.CharacterCreation.Back,
        };
        _backButton.Clicked += BackButton_Clicked;

        _propertiesPanel = new Panel(this, name: nameof(_propertiesPanel))
        {
            Dock = Pos.Right,
            DockChildSpacing = new Padding(8),
            Margin = new Margin(16, 0, 0, 0),
            ShouldDrawBackground = false,
        };

        _previewPanel = new Panel(this, name: nameof(_previewPanel))
        {
            Dock = Pos.Fill,
            ShouldDrawBackground = false,
        };

        _nameInput = new TextBox(_propertiesPanel, name: nameof(_nameInput))
        {
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            MinimumSize = new Point(240, 0),
            PlaceholderText = Strings.CharacterCreation.Name,
        };
        _nameInput.SubmitPressed += (sender, e) => TryCreateCharacter();

        _classCombobox = new LabeledComboBox(_propertiesPanel, name: nameof(_classCombobox))
        {
            AutoSizeToContents = false,
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Label = Strings.CharacterCreation.Class,
        };
        _classCombobox.ItemSelected += classCombobox_ItemSelected;

        _hairCombobox = new LabeledComboBox(_propertiesPanel, name: nameof(_hairCombobox))
        {
            AutoSizeToContents = false,
            Dock = Pos.Top,
            Font = _defaultFont,
            FontSize = 12,
            Label = Strings.CharacterCreation.Hair,
            IsHidden = true,
        };

        _hairCombobox.ItemSelected += (сontrol, args) => UpdateDisplay();

        _genderInputPanel = new Panel(_propertiesPanel, name: nameof(_genderInputPanel))
        {
            Dock = Pos.Top,
            ShouldDrawBackground = false,
        };

        // Male Checkbox
        _genderMaleCheckbox = new LabeledCheckBox(_genderInputPanel, name: nameof(_genderMaleCheckbox))
        {
            AutoSizeToContents = true,
            Dock = Pos.Left | Pos.CenterV,
            Font = _defaultFont,
            FontSize = 12,
            IsChecked = true,
            Text = Strings.CharacterCreation.Male,
        };
        _genderMaleCheckbox.Checked += MaleCheckboxGenderChecked;
        _genderMaleCheckbox.Unchecked += FemaleCheckboxGenderChecked;

        _genderFemaleCheckbox = new LabeledCheckBox(_genderInputPanel, name: nameof(_genderFemaleCheckbox))
        {
            AutoSizeToContents = true,
            Dock = Pos.Right | Pos.CenterV,
            Font = _defaultFont,
            FontSize = 12,
            Text = Strings.CharacterCreation.Female,
        };
        _genderFemaleCheckbox.Checked += FemaleCheckboxGenderChecked;
        _genderFemaleCheckbox.Unchecked += MaleCheckboxGenderChecked;

        _previewContainer = new Panel(_previewPanel, name: nameof(_previewContainer))
        {
            Dock = Pos.Fill,
            ShouldDrawBackground = false,
        };

        _prevSpriteButton = new Button(_previewContainer, name: nameof(_prevSpriteButton), disableText: true)
        {
            MinimumSize = new Point(24, 28),
            MaximumSize = new Point(24, 28),
        };
        _prevSpriteButton.Clicked += _prevSpriteButton_Clicked;
        _prevSpriteButton.SetStateTexture(ComponentState.Normal, "button.arrow_left.normal.png");
        _prevSpriteButton.SetStateTexture(ComponentState.Disabled, "button.arrow_left.disabled.png");
        _prevSpriteButton.SetStateTexture(ComponentState.Hovered, "button.arrow_left.hovered.png");
        _prevSpriteButton.SetStateTexture(ComponentState.Active, "button.arrow_left.active.png");

        _nextSpriteButton = new Button(_previewContainer, name: nameof(_nextSpriteButton), disableText: true)
        {
            MinimumSize = new Point(24, 28),
            MaximumSize = new Point(24, 28),
        };
        _nextSpriteButton.Clicked += _nextSpriteButton_Clicked;
        _nextSpriteButton.SetStateTexture(ComponentState.Normal, "button.arrow_right.normal.png");
        _nextSpriteButton.SetStateTexture(ComponentState.Disabled, "button.arrow_right.disabled.png");
        _nextSpriteButton.SetStateTexture(ComponentState.Hovered, "button.arrow_right.hovered.png");
        _nextSpriteButton.SetStateTexture(ComponentState.Active, "button.arrow_right.active.png");

        _preview = new ImagePanel(_previewContainer, name: nameof(_preview))
        {
            Alignment = [Alignments.Center],
            MaintainAspectRatio = true,
            TextureFilename = "character_preview_background.png",
        };

        _prevHairButton = new Button(_previewContainer, name: nameof(_prevHairButton), disableText: true)
        {
            MinimumSize = new Point(24, 28),
            MaximumSize = new Point(24, 28),
        };
        _prevHairButton.Clicked += PrevHairButton_Clicked;
        _prevHairButton.SetStateTexture(ComponentState.Normal, "button.arrow_left.normal.png");
        _prevHairButton.SetStateTexture(ComponentState.Disabled, "button.arrow_left.disabled.png");
        _prevHairButton.SetStateTexture(ComponentState.Hovered, "button.arrow_left.hovered.png");
        _prevHairButton.SetStateTexture(ComponentState.Active, "button.arrow_left.active.png");

        _nextHairButton = new Button(_previewContainer, name: nameof(_nextHairButton), disableText: true)
        {
            MinimumSize = new Point(24, 28),
            MaximumSize = new Point(24, 28),
        };
        _nextHairButton.Clicked += NextHairButton_Clicked;
        _nextHairButton.SetStateTexture(ComponentState.Normal, "button.arrow_right.normal.png");
        _nextHairButton.SetStateTexture(ComponentState.Disabled, "button.arrow_right.disabled.png");
        _nextHairButton.SetStateTexture(ComponentState.Hovered, "button.arrow_right.hovered.png");
        _nextHairButton.SetStateTexture(ComponentState.Active, "button.arrow_right.active.png");

        _buttonsPanel.SizeToChildren(recursive: true);
        _propertiesPanel.SizeToChildren(recursive: true);
    }

    protected override void EnsureInitialized()
    {
        SizeToChildren(recursive: true);

        LoadJsonUi(GameContentManager.UI.Menu, Graphics.Renderer?.GetResolutionString());
        _hairCombobox.IsHidden = true;
        _hairCombobox.Dock = Pos.None;
        _hairCombobox.SetBounds(0, 0, 0, 0);
        _hairCombobox.Disable();

        _classCombobox.ClearItems();

        var classDescriptors = ClassDescriptor.Lookup.Values.OfType<ClassDescriptor>()
            .Where(classDescriptor => !classDescriptor.Locked)
            .ToArray();

        foreach (var classDescriptor in classDescriptors)
        {
            _ = _classCombobox.AddItem(classDescriptor.Name, userData: classDescriptor);
        }

        ApplicationContext.Context.Value?.Logger.LogDebug(
            "Added {ClassCount} classes to the {WindowType}",
            classDescriptors.Length,
            typeof(CharacterCreationWindow).GetName(qualified: true)
        );

        LoadClass();
        UpdateDisplay();
    }

    public void Update()
    {
        if (!Networking.Network.IsConnected)
        {
            Hide();
            _mainMenu.Show();
            return;
        }

        // Re-Enable our buttons if we're not waiting for the server anymore with it disabled.
        if (!Globals.WaitingOnServer && _createButton.IsDisabled)
        {
            _createButton.Enable();
        }
    }

    public override void Show() => Show(force: false);

    public void Show(bool force)
    {
        _backButton.IsVisibleInTree = true;
        _createButton.Alignment = force ? [Alignments.Center] : [Alignments.Left];
        _hairCombobox.IsHidden = true;
        _hairCombobox.SetBounds(0, 0, 0, 0);

        _renderLayers = new ImagePanel[Options.Instance.Equipment.Paperdoll.Down.Count];
        for (var i = 0; i < _renderLayers.Length; i++)
        {
            _renderLayers[i] = new ImagePanel(_preview)
            {
                Alignment = [Alignments.Center],
            };
        }

        UpdateNavigationButtons();
        base.Show();
    }

    //Methods
    private void UpdateDisplay()
    {
        var classDescriptor = GetClass();

        if (classDescriptor == default || _displaySpriteIndex == -1 || classDescriptor.Sprites.Count <= 0)
        {
            foreach (var renderLayer in _renderLayers)
            {
                renderLayer.IsVisibleInTree = false;
            }

            UpdateNavigationButtons();
            return;
        }

        var source = _genderMaleCheckbox.IsChecked ? _maleSprites[_displaySpriteIndex] : _femaleSprites[_displaySpriteIndex];

        var faceTexture = GameContentManager.Current.GetTexture(TextureType.Face, source.Value.Face);
        if (faceTexture != default && string.IsNullOrEmpty(GetSelectedHair()))
        {
            var faceLayer = _renderLayers[0];
            var faceScale = Math.Min(
                _preview.InnerWidth / (double)faceTexture.Width,
                _preview.InnerHeight / (double)faceTexture.Height
            );
            var faceTextureWidth = (int)(faceTexture.Width * faceScale);
            var faceTextureHeight = (int)(faceTexture.Height * faceScale);
            var x = (_preview.Width - faceTextureWidth) / 2;
            var y = (_preview.Height - faceTextureHeight) / 2;
            faceLayer.ResetUVs();
            faceLayer.SetBounds(x, y, faceTextureWidth, faceTextureHeight);
            faceLayer.Texture = faceTexture;
            faceLayer.IsVisibleInTree = true;

            foreach (var renderLayer in _renderLayers.Skip(1))
            {
                renderLayer.IsVisibleInTree = false;
            }

            return;
        }

        // we are rendering the player facing down, then we need to know the render order of the equipments
        for (var paperdollLayerIndex = 0; paperdollLayerIndex < Options.Instance.Equipment.Paperdoll.Down.Count; paperdollLayerIndex++)
        {
            var paperdollLayerType = Options.Instance.Equipment.Paperdoll.Down[paperdollLayerIndex];
            var paperdollContainer = _renderLayers[paperdollLayerIndex];

            // handle player/equip rendering, we just need to find the correct texture
            if (string.Equals("Player", paperdollLayerType, StringComparison.Ordinal))
            {
                var spriteSource = source.Value.Sprite;
                var spriteTex = Globals.ContentManager.GetTexture(TextureType.Entity, spriteSource);
                paperdollContainer.Texture = spriteTex;
            }
            else if (string.Equals("Hair", paperdollLayerType, StringComparison.Ordinal))
            {
                var hairSource = GetSelectedHair();
                if (!string.IsNullOrEmpty(hairSource) && !string.Equals(hairSource, Strings.General.None, StringComparison.Ordinal))
                {
                    paperdollContainer.Texture = ResolveHairTexture(hairSource);
                }
                else
                {
                    paperdollContainer.Texture = default;
                }
            }
            else
            {
                paperdollContainer.Texture = default;
                continue;
                // if (paperdollLayerIndex >= selectedPreviewMetadata.Equipment.Length)
                // {
                //     continue;
                // }
                //
                // var equipFragment = selectedPreviewMetadata.Equipment[paperdollLayerIndex];
                //
                // if (equipFragment == default)
                // {
                //     paperdollContainer.Texture = default;
                //     continue;
                // }
                //
                // paperdollContainer.Texture = Globals.ContentManager.GetTexture(TextureType.Paperdoll, equipFragment.Name);
                //
                // if (paperdollContainer.Texture != default)
                // {
                //     paperdollContainer.RenderColor = equipFragment.RenderColor;
                // }
            }

            var layerTexture = paperdollContainer.Texture;
            if (layerTexture == default)
            {
                paperdollContainer.Hide();
                continue;
            }

            var imgWidth = layerTexture.Width;
            var imgHeight = layerTexture.Height;
            var textureWidth = imgWidth / Options.Instance.Sprites.NormalFrames;
            var textureHeight = imgHeight / Options.Instance.Sprites.Directions;

            paperdollContainer.SetTextureRect(0, 0, textureWidth, textureHeight);
            _ = paperdollContainer.SetSize(textureWidth, textureHeight);

            var centerX = (_preview.Width / 2) - (paperdollContainer.Width / 2);
            var centerY = (_preview.Height / 2) - (paperdollContainer.Height / 2);
            paperdollContainer.SetPosition(centerX, centerY);

            paperdollContainer.Show();
            paperdollContainer.BringToFront();
        }

        UpdateNavigationButtons();
    }

    private ClassDescriptor? GetClass()
    {
        if (_classCombobox.SelectedItem == null)
        {
            return null;
        }

        return ClassDescriptor.Lookup.Values.OfType<ClassDescriptor>().FirstOrDefault(
            descriptor =>
                !descriptor.Locked &&
                string.Equals(_classCombobox.SelectedItem.Text, descriptor.Name, StringComparison.Ordinal)
        );
    }

    private void LoadClass()
    {
        var cls = GetClass();
        _maleSprites.Clear();
        _femaleSprites.Clear();
        _availableHairs.Clear();
        _displaySpriteIndex = -1;
        _selectedHairIndex = -1;
        if (cls != null)
        {
            for (var i = 0; i < cls.Sprites.Count; i++)
            {
                if (cls.Sprites[i].Gender == 0)
                {
                    _maleSprites.Add(new KeyValuePair<int, ClassSprite>(i, cls.Sprites[i]));
                }
                else
                {
                    _femaleSprites.Add(new KeyValuePair<int, ClassSprite>(i, cls.Sprites[i]));
                }
            }

            _hairCombobox.ClearItems();
            _ = _hairCombobox.AddItem(Strings.General.None);
            foreach (var hair in cls.Hairs
                         .Where(hair => !string.IsNullOrWhiteSpace(hair))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _availableHairs.Add(hair);
                _ = _hairCombobox.AddItem(hair);
            }
            ResetHair();
        }

        ResetSprite();
        UpdateNavigationButtons();
    }

    private IGameTexture? ResolveHairTexture(string hairSource)
    {
        var hairTexture = Globals.ContentManager.GetTexture(TextureType.Hair, hairSource);
        if (hairTexture != default)
        {
            return hairTexture;
        }

        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(hairSource);
        return Globals.ContentManager.GetTexture(TextureType.Hair, $"{filenameWithoutExtension}_{SpriteAnimations.Normal}.png");
    }

    private void ResetSprite()
    {
        _nextSpriteButton.IsHidden = true;
        _prevSpriteButton.IsHidden = true;

        if (_genderMaleCheckbox.IsChecked)
        {
            if (_maleSprites.Count > 0)
            {
                _displaySpriteIndex = 0;
                if (_maleSprites.Count > 1)
                {
                    _nextSpriteButton.IsHidden = false;
                    _prevSpriteButton.IsHidden = false;
                }
            }
            else
            {
                _displaySpriteIndex = -1;
            }
        }
        else
        {
            if (_femaleSprites.Count > 0)
            {
                _displaySpriteIndex = 0;
                if (_femaleSprites.Count > 1)
                {
                    _nextSpriteButton.IsHidden = false;
                    _prevSpriteButton.IsHidden = false;
                }
            }
            else
            {
                _displaySpriteIndex = -1;
            }
        }
    }

    private string GetSelectedHair()
    {
        if (_selectedHairIndex < 0 || _selectedHairIndex >= _availableHairs.Count)
        {
            return string.Empty;
        }

        return _availableHairs[_selectedHairIndex];
    }

    private void UpdateNavigationButtons()
    {
        var hasMultipleHairs = _availableHairs.Count > 1;
        _prevHairButton.IsHidden = !hasMultipleHairs;
        _nextHairButton.IsHidden = !hasMultipleHairs;

        if (_previewContainer.Width <= 0 || _previewContainer.Height <= 0)
        {
            return;
        }

        var previewLeft = _preview.X;
        var previewTop = _preview.Y;
        var previewRight = _preview.X + _preview.Width;
        var previewWidth = _preview.Width;
        var previewHeight = _preview.Height;
        var previewCenterX = previewLeft + previewWidth / 2;

        var topButtonY = Math.Max(0, previewTop - _prevHairButton.Height - 8);
        var bottomButtonY = Math.Min(
            _previewContainer.Height - _prevSpriteButton.Height,
            previewTop + (int)(previewHeight * 0.62f)
        );

        var upperHorizontalOffset = Math.Max(18, previewWidth / 6);
        var lowerHorizontalOffset = Math.Max(12, previewWidth / 8);

        _prevHairButton.SetPosition(previewCenterX - upperHorizontalOffset - _prevHairButton.Width, topButtonY);
        _nextHairButton.SetPosition(previewCenterX + upperHorizontalOffset, topButtonY);
        _prevSpriteButton.SetPosition(Math.Max(0, previewLeft - _prevSpriteButton.Width - lowerHorizontalOffset), bottomButtonY);
        _nextSpriteButton.SetPosition(
            Math.Min(_previewContainer.Width - _nextSpriteButton.Width, previewRight + lowerHorizontalOffset),
            bottomButtonY
        );

        _prevHairButton.BringToFront();
        _nextHairButton.BringToFront();
        _prevSpriteButton.BringToFront();
        _nextSpriteButton.BringToFront();
    }

    private void SelectHairOffset(int offset)
    {
        if (_availableHairs.Count <= 1)
        {
            return;
        }

        if (_selectedHairIndex < 0)
        {
            _selectedHairIndex = 0;
        }

        _selectedHairIndex += offset;
        if (_selectedHairIndex < 0)
        {
            _selectedHairIndex = _availableHairs.Count - 1;
        }
        else if (_selectedHairIndex >= _availableHairs.Count)
        {
            _selectedHairIndex = 0;
        }
        UpdateDisplay();
    }

    private void ResetHair()
    {
        _selectedHairIndex = _availableHairs.Count > 0 ? 0 : -1;
    }

    private void _prevSpriteButton_Clicked(Base sender, MouseButtonState arguments)
    {
        _displaySpriteIndex--;
        if (_genderMaleCheckbox.IsChecked)
        {
            if (_maleSprites.Count > 0)
            {
                if (_displaySpriteIndex == -1)
                {
                    _displaySpriteIndex = _maleSprites.Count - 1;
                }
            }
            else
            {
                _displaySpriteIndex = -1;
            }
        }
        else
        {
            if (_femaleSprites.Count > 0)
            {
                if (_displaySpriteIndex == -1)
                {
                    _displaySpriteIndex = _femaleSprites.Count - 1;
                }
            }
            else
            {
                _displaySpriteIndex = -1;
            }
        }

        UpdateDisplay();
    }

    private void _nextSpriteButton_Clicked(Base sender, MouseButtonState arguments)
    {
        _displaySpriteIndex++;
        if (_genderMaleCheckbox.IsChecked)
        {
            if (_maleSprites.Count > 0)
            {
                if (_displaySpriteIndex >= _maleSprites.Count)
                {
                    _displaySpriteIndex = 0;
                }
            }
            else
            {
                _displaySpriteIndex = -1;
            }
        }
        else
        {
            if (_femaleSprites.Count > 0)
            {
                if (_displaySpriteIndex >= _femaleSprites.Count)
                {
                    _displaySpriteIndex = 0;
                }
            }
            else
            {
                _displaySpriteIndex = -1;
            }
        }

        UpdateDisplay();
    }

    void TryCreateCharacter()
    {
        var cls = GetClass();
        if (Globals.WaitingOnServer || _displaySpriteIndex == -1 || cls == default)
        {
            return;
        }

        if (!FieldChecking.IsValidUsername(_nameInput.Text, Strings.Regex.Username))
        {
            Interface.ShowAlert(Strings.CharacterCreation.InvalidName, alertType: AlertType.Error);
            return;
        }

        var charName = _nameInput.Text;
        var spriteKey = _genderMaleCheckbox.IsChecked ? _maleSprites[_displaySpriteIndex].Key : _femaleSprites[_displaySpriteIndex].Key;
        var hair = GetSelectedHair();

        PacketSender.SendCreateCharacter(charName, cls.Id, spriteKey, hair ?? string.Empty);
        Globals.WaitingOnServer = true;
        _createButton.Disable();
        ChatboxMsg.ClearMessages();
    }

    //Input Handlers
    void classCombobox_ItemSelected(Base control, ItemSelectedEventArgs args)
    {
        LoadClass();
        UpdateDisplay();
    }

    void MaleCheckboxGenderChecked(ICheckbox sender, EventArgs arguments)
    {
        _genderMaleCheckbox.IsChecked = true;
        _genderFemaleCheckbox.IsChecked = false;
        ResetSprite();
        UpdateDisplay();
    }

    void FemaleCheckboxGenderChecked(ICheckbox sender, EventArgs arguments)
    {
        _genderFemaleCheckbox.IsChecked = true;
        _genderMaleCheckbox.IsChecked = false;
        ResetSprite();
        UpdateDisplay();
    }

    void CreateButton_Clicked(Base sender, MouseButtonState arguments)
    {
        if (Globals.WaitingOnServer)
        {
            return;
        }

        TryCreateCharacter();
    }

    private void PrevHairButton_Clicked(Base sender, MouseButtonState arguments)
    {
        SelectHairOffset(-1);
    }

    private void NextHairButton_Clicked(Base sender, MouseButtonState arguments)
    {
        SelectHairOffset(1);
    }

    private void BackButton_Clicked(Base sender, MouseButtonState arguments)
    {
        Hide();
        if (Options.Instance.Player.MaxCharacters <= 1)
        {
            //Logout
            _mainMenu.Show();
        }
        else
        {
            //Character Selection Screen
            _selectCharacterWindow.Show();
        }
    }
}
