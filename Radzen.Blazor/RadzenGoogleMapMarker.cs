using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenGoogleMapMarker component.
    /// </summary>
    public class RadzenGoogleMapMarker : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        [Parameter]
        public GoogleMapPosition Position
        {
            get => _position; set
            {
                if (value != _position)
                {
                    ParamsChanged = value.Lat != _position.Lat || value.Lng != _position.Lng;
                    _position = value;
                    _position.PositionChanged = () => ParamsChanged = true;
                }
            }
        }
        private GoogleMapPosition _position = new GoogleMapPosition() { Lat = 0, Lng = 0 };

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string Title
        {
            get => _title; set
            {
                if (value != _title)
                {
                    _title = value;
                    ParamsChanged = true;
                }
            }
        }
        private string _title;

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>The label.</value>
        [Parameter]
        public string Label
        {
            get => _label; set
            {
                if (value != _label)
                {
                    _label = value;
                    ParamsChanged = true;
                }
            }
        }
        private string _label;

        /// <summary>
        /// Gets or sets custom source for marker icon.
        /// </summary>
        /// <value>The marker icon source.</value>
        [Parameter]
        public string IconSrc
        {
            get => _iconSrc; set
            {
                if (value != _iconSrc)
                {
                    _iconSrc = value;
                    ParamsChanged = true;
                }
            }
        }
        private string _iconSrc;
        RadzenGoogleMap _map;

        /// <summary>
        /// Called when parameters are changed and the marker needs to be updated on the map 
        /// </summary>
        internal bool ParamsChanged { get; set; }

        /// <summary>
        /// Gets or sets the map.
        /// </summary>
        /// <value>The map.</value>
        [CascadingParameter]
        public RadzenGoogleMap Map
        {
            get
            {
                return _map;
            }
            set
            {
                if (_map != value)
                {
                    _map = value;
                    _map.AddMarker(this);
                }
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            Map?.RemoveMarker(this);
        }
    }
}