using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace HouraiTeahouse.SmashBrew {

    /// <summary> A ScriptableObject </summary>
    /// <seealso cref="DataManager" />
    /// <seealso cref="SceneData" />
    [CreateAssetMenu(fileName = "New Character", menuName = "SmashBrew/Character Data")]
    public class CharacterData : ExtendableObject, IGameData {

        [SerializeField]
        [ReadOnly]
        [Tooltip("The unique ID used for this character")]
        uint _id;

        [Header("General Data")]
        [SerializeField]
        [Tooltip(" Is the Character selectable from the character select screen?")]
        bool _isSelectable;

        [SerializeField]
        [Tooltip("Is the Character viewable in the character select screen?")]
        bool _isVisible;

        [SerializeField]
        [Resource(typeof(SceneData))]
        [Tooltip("The Character's associated stage.")]
        string _homeStage;

        [Header("2D Art Data")]
        [SerializeField]
        [Resource(typeof(Sprite))]
        [Tooltip("The icon used to represent the character.")]
        string _icon;

        [SerializeField, Resource(typeof(Sprite))]
        string[] _portraits;

        Resource<Sprite>[] _portraitResources;

        [SerializeField]
        [Tooltip("The center of the crop for smaller cropped views")]
        Vector2 _cropPositon;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("The size of the crop. In normalized coordinates.")]
        float _cropSize;

        [SerializeField]
        Color _backgroundColor = Color.white;

        [SerializeField]
        [Resource(typeof(GameObject))]
        [Tooltip("The prefab of the Character to spawn.")]
        string _prefab;

        [Header("Text Data")]
        [SerializeField]
        string _shortName;

        [SerializeField]
        string _fullName;

        [Header("Audio Data")]
        [SerializeField]
        [Resource(typeof(AudioClip))]
        [Tooltip("The audio clip played for the Character's announer")]
        string _announcerClip;

        [SerializeField]
        [Resource(typeof(AudioClip))]
        [Tooltip("The theme played on the match results screen when the character wins")]
        string _victoryTheme;

        /// <summary> The short name of the character. Usually just their first name. </summary>
        public string ShortName {
            get { return _shortName; }
        }

        /// <summary> The full name of the character. </summary>
        public string FullName {
            get { return _fullName; }
        }

        /// <summary> Gets how many palletes </summary>
        public int PalleteCount {
            get { return _portraits == null ? 0 : _portraits.Length; }
        }

        /// <summary> Gets the resource for the character's icon </summary>
        public Resource<Sprite> Icon { get; private set; }

        /// <summary> Get the resource for the character's home stage </summary>
        public Resource<SceneData> HomeStage { get; private set; }

        /// <summary> Gets the resource for the character's prefab </summary>
        public Resource<GameObject> Prefab { get; private set; }

        /// <summary> Gets the resource for the character's announcer clip </summary>
        public Resource<AudioClip> Announcer { get; private set; }

        /// <summary> Gets the resource for the character's victory theme clip </summary>
        public Resource<AudioClip> VictoryTheme { get; private set; }

        /// <summary> The color used in the character's select image </summary>
        public Color BackgroundColor {
            get { return _backgroundColor; }
        }

        /// <summary> Is the Character selectable from the character select screen? </summary>
        public bool IsSelectable {
            get { return _isSelectable && _isVisible; }
        }

        /// <summary> Is the Character viewable in the character select screen? </summary>
        public bool IsVisible {
            get { return _isVisible; }
        }

        public uint Id {
            get { return _id; }
        }

        public void Unload() {
            Icon.Unload();
            Prefab.Unload();
            HomeStage.Unload();
            VictoryTheme.Unload();
            foreach (Resource<Sprite> portrait in _portraitResources)
                portrait.Unload();
        }

        /// <summary> Gets the crop rect relative to a texture </summary>
        /// <param name="texture"> the texture to get the rect relative to </param>
        /// <returns> the crop rect </returns>
        public Rect CropRect(Texture texture) {
            if (!texture)
                return new Rect(0, 0, 1, 1);
            float extents = _cropSize / 2;
            return
                texture.UVToPixelRect(new Rect(_cropPositon.x - extents, _cropPositon.y - extents, _cropSize, _cropSize));
        }

        /// <summary> Gets the resource for the sprite portrait for a certain pallete. </summary>
        /// <param name="pallete"> the pallete color to choose </param>
        /// <exception cref="ArgumentException"> <paramref name="pallete" /> is less than 0 or greater than
        /// <see cref="PalleteCount" /> </exception>
        /// <returns> </returns>
        public Resource<Sprite> GetPortrait(int pallete) {
            Argument.Check("pallete", Check.Range(pallete, PalleteCount));
            if (_portraitResources == null || _portraits.Length != _portraitResources.Length)
                RegeneratePortraits();
            return _portraitResources[pallete];
        }

        /// <summary> Unity Callback. Called when the asset instance is loaded into memory. </summary>
        void OnEnable() {
            if (_portraits == null)
                return;
            Icon = Resource.Get<Sprite>(_icon);
            Prefab = Resource.Get<GameObject>(_prefab);
            HomeStage = Resource.Get<SceneData>(_homeStage);
            Announcer = Resource.Get<AudioClip>(_announcerClip);
            VictoryTheme = Resource.Get<AudioClip>(_victoryTheme);
            RegeneratePortraits();
        }

        /// <summary> Unity callback. Called when the asset instance is unloaded from memory. </summary>
        void OnDisable() {
            Unload();
        }

        void Reset() {
            RegenerateID();
        }

        void RegeneratePortraits() { _portraitResources = _portraits.Select(Resource.Get<Sprite>).ToArray(); }
        [ContextMenu("Regenerate ID")]
        void RegenerateID() { _id = (uint)new Random().Next();}

    }

}
