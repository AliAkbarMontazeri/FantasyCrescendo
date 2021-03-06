using UnityEngine;
using UnityEngine.Networking;

namespace HouraiTeahouse.SmashBrew.Matches {

    /// <summary> An abstract class  to define a Match Rule. These instances are searched for before the start of a Match to
    /// define the rules of a match. They run as normal MonoBehaviours, but are regularly polled for </summary>
    [RequireComponent(typeof(Match))]
    public abstract class MatchRule : NetworkBehaviour {

        [SyncVar, SerializeField, ReadOnly]
        bool _isActive;

        /// <summary> The PlayerPrefs key to check for whether the rule is used or not. Stored as an integer. If 0, the rule is
        /// disabled. If any other number, it is enabled. If the key does not exist. The rule remains in whatever state it was left
        /// in the editor. </summary>
        [SerializeField]
        string _playerPrefCheck;

        /// <summary> A refernce to the central Match object. </summary>
        protected static Match Match { get; private set; }

        public bool IsActive {
            get { return _isActive; }
            protected set { _isActive = value; }
        }

        /// <summary> Unity Callback. Called on object instantiation. </summary>
        protected virtual void Start() {
            Log.Info("Match rule enabled: {0}".With(ToString()));
        }

        protected virtual void Awake() {
            Match = this.SafeGetComponent<Match>();
#if !UNITY_EDITOR
            if (Prefs.Exists(_playerPrefCheck))
                _isActive = Prefs.GetBool(_playerPrefCheck);
#endif
        }

        public override void OnStartServer() {
            base.OnStartServer();
            _isActive = true;
        }

        /// <summary> Gets the winner of the Match, according to the Match Rule. </summary>
        /// <returns> the Player that won. Null if there is a tie, or no winner is declared. </returns>
        public abstract Player GetWinner();

    }

}
