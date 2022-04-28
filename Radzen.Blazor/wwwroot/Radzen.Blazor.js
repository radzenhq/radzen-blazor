if (!Element.prototype.matches) {
  Element.prototype.matches =
    Element.prototype.msMatchesSelector ||
    Element.prototype.webkitMatchesSelector;
}

if (!Element.prototype.closest) {
  Element.prototype.closest = function (s) {
    var el = this;

    do {
      if (el.matches(s)) return el;
      el = el.parentElement || el.parentNode;
    } while (el !== null && el.nodeType === 1);
    return null;
  };
}

var resolveCallbacks = [];
var rejectCallbacks = [];

window.Radzen = {
    mask: function (id, mask, pattern, characterPattern) {
      var el = document.getElementById(id);
      if (el) {
          var format = function (value, mask, pattern, characterPattern) {
              var chars = !characterPattern ? value.replace(new RegExp(pattern, "g"), "").split('') : value.match(new RegExp(characterPattern, "g"));
              var count = 0;

              var formatted = '';
              for (var i = 0; i < mask.length; i++) {
                  const c = mask[i];
                  if (chars && chars[count]) {
                      if (/\*/.test(c)) {
                          formatted += chars[count];
                          count++;
                      } else {
                          formatted += c;
                      }
                  }
              }
              return formatted;
          }
          el.value = format(el.value, mask, pattern, characterPattern);
      }
  },
  addContextMenu: function (id, ref) {
     var el = document.getElementById(id);
     if (el) {
        var handler = function (e) {
            e.stopPropagation();
            e.preventDefault(); 
            ref.invokeMethodAsync('RadzenComponent.RaiseContextMenu',
                {
                    ClientX: e.clientX,
                    ClientY: e.clientY,
                    ScreenX: e.screenX,
                    ScreenY: e.screenY,
                    AltKey: e.altKey,
                    ShiftKey: e.shiftKey,
                    CtrlKey: e.ctrlKey,
                    MetaKey: e.metaKey,
                    Button: e.button,
                    Buttons: e.buttons,
                });
            return false;
        };
        Radzen[id + 'contextmenu'] = handler;
        el.addEventListener('contextmenu', handler, false);
     }
  },
  addMouseEnter: function (id, ref) {
     var el = document.getElementById(id);
     if (el) {
        var handler = function (e) {
            ref.invokeMethodAsync('RadzenComponent.RaiseMouseEnter');
        };
        Radzen[id + 'mouseenter'] = handler;
        el.addEventListener('mouseenter', handler, false);
     }
  },
  addMouseLeave: function (id, ref) {
     var el = document.getElementById(id);
     if (el) {
        var handler = function (e) {
            ref.invokeMethodAsync('RadzenComponent.RaiseMouseLeave');;
        };
        Radzen[id + 'mouseleave'] = handler;
        el.addEventListener('mouseleave', handler, false);
     }
  },
  removeContextMenu: function (id) {
      var el = document.getElementById(id);
      if (el && Radzen[id + 'contextmenu']) {
          el.removeEventListener('contextmenu', Radzen[id + 'contextmenu']);
      }
  },
  removeMouseEnter: function (id) {
      var el = document.getElementById(id);
      if (el && Radzen[id + 'mouseenter']) {
          el.removeEventListener('mouseenter', Radzen[id + 'mouseenter']);
      }
  },
  removeMouseLeave: function (id) {
      var el = document.getElementById(id);
      if (el && Radzen[id + 'mouseleave']) {
          el.removeEventListener('mouseleave', Radzen[id + 'mouseleave']);
      }
  },
  adjustDataGridHeader: function (scrollableHeader, scrollableBody) {
    if (scrollableHeader && scrollableBody) {
      scrollableHeader.style.cssText =
        scrollableBody.clientHeight < scrollableBody.scrollHeight
          ? 'margin-left:0px;padding-right: ' +
            (scrollableBody.offsetWidth - scrollableBody.clientWidth) +
            'px'
          : 'margin-left:0px;';
    }
  },
  preventDefaultAndStopPropagation: function (e) {
    e.preventDefault();
    e.stopPropagation();
  },
  preventArrows: function (el) {
    var preventDefault = function (e) {
      if (e.keyCode === 38 || e.keyCode === 40) {
        e.preventDefault();
        return false;
      }
    };
    if (el) {
       el.addEventListener('keydown', preventDefault, false);
    }
  },
  selectTab: function (id, index) {
    var el = document.getElementById(id);
    if (el && el.parentNode && el.parentNode.previousElementSibling) {
        var count = el.parentNode.children.length;
        for (var i = 0; i < count; i++) {
            var content = el.parentNode.children[i];
            if (content) {
                content.style.display = i == index ? 'block' : 'none';
            }
            var header = el.parentNode.previousElementSibling.children[i];
            if (header) {
                if (i == index) {
                    header.classList.add('rz-tabview-selected');
                }
                else {
                    header.classList.remove('rz-tabview-selected');
                }
            }
        }
    }
  },
  loadGoogleMaps: function (defaultView, apiKey, resolve, reject) {
    resolveCallbacks.push(resolve);
    rejectCallbacks.push(reject);

    if (defaultView['rz_map_init']) {
      return;
    }

    defaultView['rz_map_init'] = function () {
      for (var i = 0; i < resolveCallbacks.length; i++) {
        resolveCallbacks[i](defaultView.google);
      }
    };

    var document = defaultView.document;
    var script = document.createElement('script');

    script.src =
      'https://maps.googleapis.com/maps/api/js?' +
      (apiKey ? 'key=' + apiKey + '&' : '') +
      'callback=rz_map_init';

    script.async = true;
    script.defer = true;
    script.onerror = function (err) {
      for (var i = 0; i < rejectCallbacks.length; i++) {
        rejectCallbacks[i](err);
      }
    };

    document.body.appendChild(script);
  },
  createMap: function (wrapper, ref, id, apiKey, zoom, center, markers) {
    var api = function () {
      var defaultView = document.defaultView;

      return new Promise(function (resolve, reject) {
        if (defaultView.google && defaultView.google.maps) {
          return resolve(defaultView.google);
        }

        Radzen.loadGoogleMaps(defaultView, apiKey, resolve, reject);
      });
    };

    api().then(function (google) {
      Radzen[id] = ref;
      Radzen[id].google = google;

      Radzen[id].instance = new google.maps.Map(wrapper, {
        center: center,
        zoom: zoom
      });

      Radzen[id].instance.addListener('click', function (e) {
        Radzen[id].invokeMethodAsync('RadzenGoogleMap.OnMapClick', {
          Position: {Lat: e.latLng.lat(), Lng: e.latLng.lng()}
        });
      });

      Radzen.updateMap(id, zoom, center, markers);
    });
  },
  updateMap: function (id, zoom, center, markers) {
    if (Radzen[id] && Radzen[id].instance) {
      if (Radzen[id].instance.markers && Radzen[id].instance.markers.length) {
        for (var i = 0; i < Radzen[id].instance.markers.length; i++) {
          Radzen[id].instance.markers[i].setMap(null);
        }
      }

      Radzen[id].instance.markers = [];

      markers.forEach(function (m) {
        var marker = new this.google.maps.Marker({
          position: m.position,
          title: m.title,
          label: m.label
        });

        marker.addListener('click', function (e) {
          Radzen[id].invokeMethodAsync('RadzenGoogleMap.OnMarkerClick', {
            Title: marker.title,
            Label: marker.label,
            Position: marker.position
          });
        });

        marker.setMap(Radzen[id].instance);

        Radzen[id].instance.markers.push(marker);
      });

      Radzen[id].instance.setZoom(zoom);

      Radzen[id].instance.setCenter(center);
    }
  },
  destroyMap: function (id) {
    if (Radzen[id].instance) {
      delete Radzen[id].instance;
    }
  },
  createSlider: function (
    id,
    slider,
    parent,
    range,
    minHandle,
    maxHandle,
    min,
    max,
    value,
    step
  ) {
    Radzen[id] = {};
    Radzen[id].mouseMoveHandler = function (e) {
      if (!slider.canChange) return;
      e.preventDefault();
      var handle = slider.isMin ? minHandle : maxHandle;
      var offsetX =
        e.targetTouches && e.targetTouches[0]
          ? e.targetTouches[0].pageX - e.target.getBoundingClientRect().left
          : e.pageX - handle.getBoundingClientRect().left;
      var percent = (handle.offsetLeft + offsetX) / parent.offsetWidth;

      if (percent > 1) {
          percent = 1;
      } else if (percent < 0) {
          percent = 0;
      }

      var newValue = percent * (max - min) + min;
      var oldValue = range ? value[slider.isMin ? 0 : 1] : value;

      if (
        slider.canChange &&
        newValue >= min &&
        newValue <= max &&
        newValue != oldValue
      ) {
        slider.invokeMethodAsync(
          'RadzenSlider.OnValueChange',
          newValue,
          !!slider.isMin
        );
      }
    };

    Radzen[id].mouseDownHandler = function (e) {
      if (parent.classList.contains('rz-state-disabled')) return;
      if (minHandle == e.target || maxHandle == e.target) {
        slider.canChange = true;
        slider.isMin = minHandle == e.target;
      } else {
        var offsetX =
          e.targetTouches && e.targetTouches[0]
            ? e.targetTouches[0].pageX - e.target.getBoundingClientRect().left
            : e.offsetX;
        var percent = offsetX / parent.offsetWidth;
        var newValue = percent * max;
        var oldValue = range ? value[slider.isMin ? 0 : 1] : value;
        if (newValue >= min && newValue <= max && newValue != oldValue) {
          slider.invokeMethodAsync(
            'RadzenSlider.OnValueChange',
            newValue,
            !!slider.isMin
          );
        }
      }
    };

    Radzen[id].mouseUpHandler = function (e) {
      slider.canChange = false;
    };

    document.addEventListener('mousemove', Radzen[id].mouseMoveHandler);
    document.addEventListener('touchmove', Radzen[id].mouseMoveHandler, {
      passive: false, capture: true
    });

    document.addEventListener('mouseup', Radzen[id].mouseUpHandler);
    document.addEventListener('touchend', Radzen[id].mouseUpHandler, {
      passive: true
    });

    parent.addEventListener('mousedown', Radzen[id].mouseDownHandler);
    parent.addEventListener('touchstart', Radzen[id].mouseDownHandler, {
      passive: true
    });
  },
  destroySlider: function (id, parent) {
    if (!Radzen[id]) return;

    if (Radzen[id].mouseMoveHandler) {
      document.removeEventListener('mousemove', Radzen[id].mouseMoveHandler);
      document.removeEventListener('touchmove', Radzen[id].mouseMoveHandler);
      delete Radzen[id].mouseMoveHandler;
    }
    if (Radzen[id].mouseUpHandler) {
      document.removeEventListener('mouseup', Radzen[id].mouseUpHandler);
      document.removeEventListener('touchend', Radzen[id].mouseUpHandler);
      delete Radzen[id].mouseUpHandler;
    }
    if (Radzen[id].mouseDownHandler) {
      parent.removeEventListener('mousedown', Radzen[id].mouseDownHandler);
      parent.removeEventListener('touchstart', Radzen[id].mouseDownHandler);
      delete Radzen[id].mouseDownHandler;
    }

    Radzen[id] = null;
  },
  focusElement: function (elementId) {
    var el = document.getElementById(elementId);
    if (el) {
      el.focus();
    }
  },
  selectListItem: function (input, ul, index) {
    if (!input || !ul) return;

    var childNodes = ul.getElementsByTagName('LI');

    var highlighted = ul.querySelectorAll('.rz-state-highlight');
    if (highlighted.length) {
      for (var i = 0; i < highlighted.length; i++) {
        highlighted[i].classList.remove('rz-state-highlight');
      }
    }

    ul.nextSelectedIndex = index;

    if (
      ul.nextSelectedIndex >= 0 &&
      ul.nextSelectedIndex <= childNodes.length - 1
    ) {
      childNodes[ul.nextSelectedIndex].classList.add('rz-state-highlight');
      if (ul.parentNode.classList.contains('rz-autocomplete-panel')) {
        ul.parentNode.scrollTop = childNodes[ul.nextSelectedIndex].offsetTop;
      } else {
        ul.parentNode.scrollTop =
          childNodes[ul.nextSelectedIndex].offsetTop - ul.parentNode.offsetTop;
      }
    }
  },
  focusListItem: function (input, ul, isDown, startIndex) {
    if (!input || !ul) return;

    var childNodes = ul.getElementsByTagName('LI');

    if (!childNodes || childNodes.length == 0) return;

    if (startIndex == undefined || startIndex == null) {
      startIndex = -1;
    }

    ul.nextSelectedIndex = startIndex;

    if (isDown) {
      if (ul.nextSelectedIndex < childNodes.length - 1) {
        ul.nextSelectedIndex++;
      }
    } else {
      if (ul.nextSelectedIndex > 0) {
        ul.nextSelectedIndex--;
      }
    }

    var highlighted = ul.querySelectorAll('.rz-state-highlight');
    if (highlighted.length) {
      for (var i = 0; i < highlighted.length; i++) {
        highlighted[i].classList.remove('rz-state-highlight');
      }
    }

    if (
      ul.nextSelectedIndex >= 0 &&
      ul.nextSelectedIndex <= childNodes.length - 1
    ) {
      childNodes[ul.nextSelectedIndex].classList.add('rz-state-highlight');
      if (ul.parentNode.classList.contains('rz-autocomplete-panel')) {
        ul.parentNode.scrollTop = childNodes[ul.nextSelectedIndex].offsetTop;
      } else {
        ul.parentNode.scrollTop =
          childNodes[ul.nextSelectedIndex].offsetTop - ul.parentNode.offsetTop;
      }
    }

    return ul.nextSelectedIndex;
  },
  uploadInputChange: function (e, url, auto, multiple) {
      if (auto) {
          Radzen.upload(e.target, url, multiple, true);
          e.target.value = '';
      } else {
          Radzen.uploadChange(e.target);
      }
  },
  uploads: function (uploadComponent, id) {
    if (!Radzen.uploadComponents) {
      Radzen.uploadComponents = {};
    }
    Radzen.uploadComponents[id] = uploadComponent;
  },
  uploadChange: function (fileInput) {
    var files = [];
    for (var i = 0; i < fileInput.files.length; i++) {
      var file = fileInput.files[i];
      files.push({
        Name: file.name,
        Size: file.size,
        Url: URL.createObjectURL(file)
      });
    }

    var uploadComponent =
      Radzen.uploadComponents && Radzen.uploadComponents[fileInput.id];
    if (uploadComponent) {
      if (uploadComponent.localFiles) {
        // Clear any previously created preview URL(s)
        for (var i = 0; i < uploadComponent.localFiles.length; i++) {
          var file = uploadComponent.localFiles[i];
          if (file.Url) {
            URL.revokeObjectURL(file.Url);
          }
        }
      }

      uploadComponent.files = Array.from(fileInput.files);
      uploadComponent.localFiles = files;
      uploadComponent.invokeMethodAsync('RadzenUpload.OnChange', files);
    }

    for (var i = 0; i < fileInput.files.length; i++) {
      var file = fileInput.files[i];
      if (file.Url) {
        URL.revokeObjectURL(file.Url);
      }
    }
  },
  removeFileFromUpload: function (fileInput, name) {
    var uploadComponent = Radzen.uploadComponents && Radzen.uploadComponents[fileInput.id];
    if (!uploadComponent) return;
    var file = uploadComponent.files.find(function (f) { return f.name == name; })
    if (!file) { return; }
    var localFile = uploadComponent.localFiles.find(function (f) { return f.Name == name; });
    if (localFile) {
      URL.revokeObjectURL(localFile.Url);
    }
    var index = uploadComponent.files.indexOf(file)
    if (index != -1) {
        uploadComponent.files.splice(index, 1);
    }
    fileInput.value = '';
  },
  upload: function (fileInput, url, multiple, clear) {
    var uploadComponent = Radzen.uploadComponents && Radzen.uploadComponents[fileInput.id];
    if (!uploadComponent) { return; }
    if (!uploadComponent.files || clear) {
        uploadComponent.files = Array.from(fileInput.files);
    }
    var data = new FormData();
    var files = [];
    for (var i = 0; i < uploadComponent.files.length; i++) {
      var file = uploadComponent.files[i];
      data.append(multiple ? 'files' : 'file', file, file.name);
      files.push({Name: file.name, Size: file.size});
    }
    var xhr = new XMLHttpRequest();
    xhr.upload.onprogress = function (e) {
      if (e.lengthComputable) {
        var uploadComponent =
          Radzen.uploadComponents && Radzen.uploadComponents[fileInput.id];
        if (uploadComponent) {
          var progress = parseInt((e.loaded / e.total) * 100);
          uploadComponent.invokeMethodAsync(
            'RadzenUpload.OnProgress',
            progress,
            e.loaded,
            e.total,
            files
          );
        }
      }
    };
    xhr.onreadystatechange = function (e) {
      if (xhr.readyState === XMLHttpRequest.DONE) {
        var status = xhr.status;
        var uploadComponent =
          Radzen.uploadComponents && Radzen.uploadComponents[fileInput.id];
        if (uploadComponent) {
          if (status === 0 || (status >= 200 && status < 400)) {
            uploadComponent.invokeMethodAsync(
              'RadzenUpload.OnComplete',
              xhr.responseText
            );
          } else {
            uploadComponent.invokeMethodAsync(
              'RadzenUpload.OnError',
              xhr.responseText
            );
          }
        }
      }
    };
    uploadComponent.invokeMethodAsync('GetHeaders').then(function (headers) {
      xhr.open('POST', url, true);
      for (var name in headers) {
        xhr.setRequestHeader(name, headers[name]);
      }
      xhr.send(data);
    });
  },
  getCookie: function (name) {
    var value = '; ' + decodeURIComponent(document.cookie);
    var parts = value.split('; ' + name + '=');
    if (parts.length == 2) return parts.pop().split(';').shift();
  },
  getCulture: function () {
    var cultureCookie = Radzen.getCookie('.AspNetCore.Culture');
    var uiCulture = cultureCookie
      ? cultureCookie.split('|').pop().split('=').pop()
      : null;
    return uiCulture || 'en-US';
  },
  numericOnPaste: function (e) {
    if (e.clipboardData) {
      var value = e.clipboardData.getData('text');

      if (value && !isNaN(+value)) {
        return;
      }

      e.preventDefault();
    }
  },
  numericKeyPress: function (e, isInteger) {
    if (
      e.metaKey ||
      e.ctrlKey ||
      e.keyCode == 9 ||
      e.keyCode == 8 ||
      e.keyCode == 13
    ) {
      return;
    }

    var ch = String.fromCharCode(e.charCode);

    if ((isInteger ? /^[-\d]$/ : /^[-\d,.]$/).test(ch)) {
      return;
    }

    e.preventDefault();
  },
  openContextMenu: function (x,y,id) {
    Radzen.closePopup(id);

    Radzen.openPopup(null, id, false, null, x, y);
  },
  openTooltip: function (target, id, duration, position) {
    Radzen.closePopup(id);

    if (Radzen[id + 'duration']) {
      clearTimeout(Radzen[id + 'duration']);
    }

    Radzen.openPopup(target, id, false, position);

    if (duration) {
      Radzen[id + 'duration'] = setTimeout(Radzen.closePopup, duration, id);
    }
  },
  findPopup: function (id) {
    var popups = [];
    for (var i = 0; i < document.body.children.length; i++) {
      if (document.body.children[i].id == id) {
        popups.push(document.body.children[i]);
      }
    }
    return popups;
  },
  repositionPopup: function (parent, id) {
      var popup = document.getElementById(id);
      if (!popup) return;

      var rect = popup.getBoundingClientRect();
      var parentRect = parent ? parent.getBoundingClientRect() : { top: 0, bottom: 0, left: 0, right: 0, width: 0, height: 0 };

      if (/Edge/.test(navigator.userAgent)) {
          var scrollTop = document.body.scrollTop;
      } else {
          var scrollTop = document.documentElement.scrollTop;
      }

      var top = parentRect.bottom + scrollTop;

      if (top + rect.height > window.innerHeight && parentRect.top > rect.height) {
          top = parentRect.top - rect.height + scrollTop;
      }

      popup.style.top = top + 'px';
  },
  openPopup: function (parent, id, syncWidth, position, x, y, instance, callback) {
    var popup = document.getElementById(id);
    if (!popup) return;

    Radzen.activeElement = document.activeElement;

    var parentRect = parent ? parent.getBoundingClientRect() : { top: y || 0, bottom: 0, left: x || 0, right: 0, width: 0, height: 0 };

    if (/Edge/.test(navigator.userAgent)) {
      var scrollLeft = document.body.scrollLeft;
      var scrollTop = document.body.scrollTop;
    } else {
      var scrollLeft = document.documentElement.scrollLeft;
      var scrollTop = document.documentElement.scrollTop;
    }

    var top = y ? y : parentRect.bottom;
    var left = x ? x : parentRect.left;

      if (syncWidth) {
        popup.style.width = parentRect.width + 'px';
        if (!popup.style.minWidth) {
            popup.minWidth = true;
            popup.style.minWidth = parentRect.width + 'px';
        }
    } 

    popup.style.display = 'block';

    var rect = popup.getBoundingClientRect();

    var smartPosition = !position || position == 'bottom';

    if (smartPosition && top + rect.height > window.innerHeight && parentRect.top > rect.height) {
      top = parentRect.top - rect.height;

      if (position) {
        top = top - 40;
        var tooltipContent = popup.children[0];
        var tooltipContentClassName = 'rz-' + position + '-tooltip-content';
        if (tooltipContent.classList.contains(tooltipContentClassName)) {
          tooltipContent.classList.remove(tooltipContentClassName);
          tooltipContent.classList.add('rz-top-tooltip-content');
        }
      }
    }

    if (smartPosition && left + rect.width > window.innerWidth && window.innerWidth > rect.width) {
      left = window.innerWidth - rect.width;

      if (position) {
        var tooltipContent = popup.children[0];
        var tooltipContentClassName = 'rz-' + position + '-tooltip-content';
        if (tooltipContent.classList.contains(tooltipContentClassName)) {
          tooltipContent.classList.remove(tooltipContentClassName);
          tooltipContent.classList.add('rz-left-tooltip-content');
          left = parentRect.left - rect.width - 5;
          top = parentRect.top - parentRect.height;
        }
      }
    }

    if (smartPosition) {
      if (position) {
        top = top + 20;
      }
    }

    if (position == 'left') {
      left = parentRect.left - rect.width - 5;
      top =  parentRect.top;
    }

    if (position == 'right') {
      left = parentRect.right + 10;
      top = parentRect.top;
    }

    if (position == 'top') {
      top = parentRect.top - rect.height + 5;
      left = parentRect.left;
    }

    popup.style.zIndex = 2000;
    popup.style.left = left + scrollLeft + 'px';
    popup.style.top = top + scrollTop + 'px';

    if (!popup.classList.contains('rz-overlaypanel')) {
        popup.classList.add('rz-popup');
    }

    Radzen[id] = function (e) {
        if(e.type == 'contextmenu' || !e.target) return;
        if (!/Android/i.test(navigator.userAgent) && e.type == 'resize') {
            Radzen.closePopup(id, instance, callback);
            return;
        }
        var closestPopup = e.target.closest('.rz-popup');
        if (closestPopup && closestPopup != popup) {
          return;
        }
        if (parent) {
          if (e.type == 'mousedown' && !parent.contains(e.target) && !popup.contains(e.target)) {
            Radzen.closePopup(id, instance, callback);
          }
        } else {
          if (!popup.contains(e.target)) {
            Radzen.closePopup(id, instance, callback);
          }
        }
    };

    if (!Radzen.closePopupsOnScroll) {
        Radzen.closePopupsOnScroll = function (e) {
            for (var i = 0; i < Radzen.popups.length; i++) {
                var p = Radzen.popups[i];
                Radzen.closePopup(p.id, p.instance, p.callback);
            }
            Radzen.popups = [];
        };
        Radzen.popups = [];
    }

    Radzen.popups.push({id, instance, callback});

    document.body.appendChild(popup);
    document.removeEventListener('mousedown', Radzen[id]);
    document.addEventListener('mousedown', Radzen[id]);
    window.removeEventListener('resize', Radzen[id]);
    window.addEventListener('resize', Radzen[id]);

    var p = parent;
    while (p && p != document.body) {
        if (p.scrollWidth > p.clientWidth || p.scrollHeight > p.clientHeight) {
            p.removeEventListener('scroll', Radzen.closePopupsOnScroll);
            p.addEventListener('scroll', Radzen.closePopupsOnScroll);
        }
        p = p.parentElement;
    }

    if (!parent) {
        document.removeEventListener('contextmenu', Radzen[id]);
        document.addEventListener('contextmenu', Radzen[id]);
    }
  },
  closePopup: function (id, instance, callback) {
    var popup = document.getElementById(id);
    if (!popup) return;
    if (popup.style.display == 'none') return;

    if (popup) {
      if (popup.minWidth) {
          popup.style.minWidth = '';
      }
      popup.style.display = 'none';
    }
    document.removeEventListener('mousedown', Radzen[id]);
    window.removeEventListener('resize', Radzen[id]);
    Radzen[id] = null;

    if (instance) {
      instance.invokeMethodAsync(callback);
    }

    if (Radzen.activeElement && Radzen.activeElement == document.activeElement || Radzen.activeElement && document.activeElement == document.body) {
        Radzen.activeElement.focus();
        Radzen.activeElement = null;
    }
  },
  togglePopup: function (parent, id, syncWidth, instance, callback) {
    var popup = document.getElementById(id);
    if (!popup) return;
    if (popup.style.display == 'block') {
      Radzen.closePopup(id, instance, callback);
    } else {
      Radzen.openPopup(parent, id, syncWidth, null, null, null, instance, callback);
    }
  },
  destroyPopup: function (id) {
    var popup = document.getElementById(id);
    if (popup) {
      popup.parentNode.removeChild(popup);
    }
    document.removeEventListener('mousedown', Radzen[id]);
  },
  scrollDataGrid: function (e) {
    var scrollLeft =
      (e.target.scrollLeft ? '-' + e.target.scrollLeft : 0) + 'px';

      e.target.previousElementSibling.style.marginLeft = scrollLeft;
      e.target.previousElementSibling.firstElementChild.style.paddingRight =
          e.target.clientHeight < e.target.scrollHeight ? (e.target.offsetWidth - e.target.clientWidth) + 'px' : '0px';

    if (e.target.nextElementSibling) {
        e.target.nextElementSibling.style.marginLeft = scrollLeft;
        e.target.nextElementSibling.firstElementChild.style.paddingRight =
            e.target.clientHeight < e.target.scrollHeight ? (e.target.offsetWidth - e.target.clientWidth) + 'px' : '0px';
    }

    for (var i = 0; i < document.body.children.length; i++) {
        if (document.body.children[i].classList.contains('rz-overlaypanel')) {
            document.body.children[i].style.display = 'none';
        }
    }
  },
  openDialog: function (options, dialogService) {
    Radzen.dialogService = dialogService;
    if (
      document.documentElement.scrollHeight >
      document.documentElement.clientHeight
    ) {
      document.body.classList.add('no-scroll');
    }

    if (options.autoFocusFirstElement) {
        setTimeout(function () {
            var dialogs = document.querySelectorAll('.rz-dialog-content');
            if (dialogs.length == 0) return;
            var lastDialog = dialogs[dialogs.length - 1];

            if (lastDialog) {
                var focusable = lastDialog.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
                var firstFocusable = focusable[0];
                if (firstFocusable) {
                    firstFocusable.focus();
                }
            }
        }, 500);
    }
    if (options.closeDialogOnEsc) {
        document.removeEventListener('keydown', Radzen.closePopupOrDialog);
        document.addEventListener('keydown', Radzen.closePopupOrDialog);
    }
  },
  closeDialog: function () {
    document.body.classList.remove('no-scroll');
  },
  closePopupOrDialog: function (e) {
      e = e || window.event;
      var isEscape = false;
      if ("key" in e) {
          isEscape = (e.key === "Escape" || e.key === "Esc");
      } else {
          isEscape = (e.keyCode === 27);
      }
      if (isEscape && Radzen.dialogService) {
          var popups = document.querySelectorAll('.rz-popup,.rz-overlaypanel');
          for (var i = 0; i < popups.length; i++) {
              if (popups[i].style.display != 'none') {
                  return;
              }
          }

          Radzen.dialogService.invokeMethodAsync('DialogService.Close', null);
      }
  },
  getInputValue: function (arg) {
    var input =
      arg instanceof Element || arg instanceof HTMLDocument
        ? arg
        : document.getElementById(arg);
    return input ? input.value : '';
  },
  setInputValue: function (arg, value) {
    var input =
      arg instanceof Element || arg instanceof HTMLDocument
        ? arg
        : document.getElementById(arg);
    if (input) {
      input.value = value;
    }
  },
  readFileAsBase64: function (fileInput, maxFileSize, maxWidth, maxHeight) {
    var calculateWidthAndHeight = function (img) {
      var width = img.width;
      var height = img.height;
      // Change the resizing logic
      if (width > height) {
        if (width > maxWidth) {
          height = height * (maxWidth / width);
          width = maxWidth;
        }
      } else {
        if (height > maxHeight) {
          width = width * (maxHeight / height);
          height = maxHeight;
        }
      }
      return { width, height };
    };
    var readAsDataURL = function (fileInput) {
      return new Promise(function (resolve, reject) {
        var reader = new FileReader();
        reader.onerror = function () {
          reader.abort();
          reject('Error reading file.');
        };
        reader.addEventListener(
          'load',
          function () {
            if (maxWidth > 0 && maxHeight > 0) {
              var img = document.createElement("img");
              img.onload = function (event) {
                // Dynamically create a canvas element
                var canvas = document.createElement("canvas");
                var res = calculateWidthAndHeight(img);
                canvas.width = res.width;
                canvas.height = res.height;
                var ctx = canvas.getContext("2d");
                ctx.drawImage(img, 0, 0, res.width, res.height);
                resolve(canvas.toDataURL(fileInput.type));
              }
              img.src = reader.result;
            } else {
              resolve(reader.result);
            }
          },
          false
          );
        var file = fileInput.files[0];
        if (!file) return;
        if (file.size <= maxFileSize) {
          reader.readAsDataURL(file);
        } else {
          reject('File too large.');
        }
      });
    };

    return readAsDataURL(fileInput);
  },
  toggleMenuItem: function (target, event) {
    var item = target.closest('.rz-navigation-item');

    var active = !item.classList.contains('rz-navigation-item-active');

    function toggle(active) {
      item.classList.toggle('rz-navigation-item-active', active);

      target.classList.toggle('rz-navigation-item-wrapper-active', active);

      var children = item.querySelector('.rz-navigation-menu');

      if (children) {
        children.style.display = active ? '' : 'none';
      }

      var icon = item.querySelector('.rz-navigation-item-icon-children');

      if (icon) {
        var deg = active ? '180deg' : 0;
        icon.style.transform = 'rotate(' + deg + ')';
      }
    }

    toggle(active);

    document.removeEventListener('click', target.clickHandler);

    target.clickHandler = function (event) {
      if (item.contains(event.target)) {
        var child = event.target.closest('.rz-navigation-item');
        if (child && child.querySelector('.rz-navigation-menu')) {
          return;
        }
      }
      toggle(false);
    }

    document.addEventListener('click', target.clickHandler);
  },
  destroyChart: function (ref) {
    if (ref.mouseMoveHandler) {
      ref.removeEventListener('mousemove', ref.mouseMoveHandler);
      delete ref.mouseMoveHandler;
    }
    if (ref.clickHandler) {
      ref.removeEventListener('click', ref.clickHandler);
      delete ref.clickHandler;
    }

    this.destroyResizable(ref);
  },
  destroyGauge: function (ref) {
    this.destroyResizable(ref);
  },
  destroyResizable: function (ref) {
    if (ref.resizeObserver) {
      ref.resizeObserver.disconnect();
      delete ref.resizeObserver;
    }
    if (ref.resizeHandler) {
      window.removeEventListener('resize', ref.resizeHandler);
      delete ref.resizeHandler;
    }
  },
  createResizable: function (ref, instance) {
    ref.resizeHandler = function () {
      var rect = ref.getBoundingClientRect();

      instance.invokeMethodAsync('Resize', rect.width, rect.height);
    };

    if (window.ResizeObserver) {
      ref.resizeObserver = new ResizeObserver(ref.resizeHandler);
      ref.resizeObserver.observe(ref);
    } else {
      window.addEventListener('resize', ref.resizeHandler);
    }

    var rect = ref.getBoundingClientRect();

    return {width: rect.width, height: rect.height};
  },
  createChart: function (ref, instance) {
    ref.mouseMoveHandler = function (e) {
      var rect = ref.getBoundingClientRect();
      var x = e.clientX - rect.left;
      var y = e.clientY - rect.top;
      instance.invokeMethodAsync('MouseMove', x, y);
    };
    ref.clickHandler = function (e) {
      var rect = ref.getBoundingClientRect();
      var x = e.clientX - rect.left;
      var y = e.clientY - rect.top;
      instance.invokeMethodAsync('Click', x, y);
    };

    ref.addEventListener('mousemove', ref.mouseMoveHandler);
    ref.addEventListener('click', ref.clickHandler);

    return this.createResizable(ref, instance);
  },
  createGauge: function (ref, instance) {
    return this.createResizable(ref, instance);
  },
  destroyScheduler: function (ref) {
    if (ref.resizeHandler) {
      window.removeEventListener('resize', ref.resizeHandler);
      delete ref.resizeHandler;
    }
  },
  createScheduler: function (ref, instance) {
    ref.resizeHandler = function () {
      var rect = ref.getBoundingClientRect();

      instance.invokeMethodAsync('Resize', rect.width, rect.height);
    };

    window.addEventListener('resize', ref.resizeHandler);

    var rect = ref.getBoundingClientRect();
    return {width: rect.width, height: rect.height};
  },
  innerHTML: function (ref, value) {
    if (value != undefined) {
      ref.innerHTML = value;
    } else {
      return ref.innerHTML;
    }
  },
  execCommand: function (ref, name, value) {
    if (document.activeElement != ref) {
      ref.focus();
    }
    document.execCommand(name, false, value);
    return this.queryCommands(ref);
  },
  queryCommands: function (ref) {
    return {
      html: ref.innerHTML,
      fontName: document.queryCommandValue('fontName'),
      fontSize: document.queryCommandValue('fontSize'),
      formatBlock: document.queryCommandValue('formatBlock'),
      bold: document.queryCommandState('bold'),
      underline: document.queryCommandState('underline'),
      justifyRight: document.queryCommandState('justifyRight'),
      justifyLeft: document.queryCommandState('justifyLeft'),
      justifyCenter: document.queryCommandState('justifyCenter'),
      justifyFull: document.queryCommandState('justifyFull'),
      italic: document.queryCommandState('italic'),
      strikeThrough: document.queryCommandState('strikeThrough'),
      superscript: document.queryCommandState('superscript'),
      subscript: document.queryCommandState('subscript'),
      unlink: document.queryCommandEnabled('unlink'),
      undo: document.queryCommandEnabled('undo'),
      redo: document.queryCommandEnabled('redo')
    };
  },
  mediaQueries: {},
  mediaQuery: function(query, instance) {
    if (instance) {
      function callback(event) {
          instance.invokeMethodAsync('OnChange', event.matches)
      };
      var query = matchMedia(query);
      this.mediaQueries[instance._id] = function() {
          query.removeListener(callback);
      }
      query.addListener(callback);
      return query.matches;
    } else {
        instance = query;
        if (this.mediaQueries[instance._id]) {
            this.mediaQueries[instance._id]();
            delete this.mediaQueries[instance._id];
        }
    }
  },
  createEditor: function (ref, uploadUrl, paste, instance) {
    ref.inputListener = function () {
      instance.invokeMethodAsync('OnChange', ref.innerHTML);
    };
    ref.selectionChangeListener = function () {
      if (document.activeElement == ref) {
        instance.invokeMethodAsync('OnSelectionChange');
      }
    };
    ref.pasteListener = function (e) {
      var item = e.clipboardData.items[0];

      if (item.kind == 'file') {
        e.preventDefault();

        var xhr = new XMLHttpRequest();
        var data = new FormData();
        data.append("file", item.getAsFile());
        xhr.onreadystatechange = function (e) {
          if (xhr.readyState === XMLHttpRequest.DONE) {
            var status = xhr.status;
            if (status === 0 || (status >= 200 && status < 400)) {
              var result = JSON.parse(xhr.responseText);
              document.execCommand("insertHTML", false, '<img src="' + result.url + '">');
            } else {
              console.log(xhr.responseText);
            }
          }
        }
        instance.invokeMethodAsync('GetHeaders').then(function (headers) {
            xhr.open('POST', uploadUrl, true);
            for (var name in headers) {
              xhr.setRequestHeader(name, headers[name]);
            }
            xhr.send(data);
          });
      } else if (paste) {
        e.preventDefault();
        var data = e.clipboardData.getData('text/html') || e.clipboardData.getData('text/plain');

        instance.invokeMethodAsync('OnPaste', data)
          .then(function (html) {
            document.execCommand("insertHTML", false, html);
          });
      }
    };
    ref.addEventListener('input', ref.inputListener);
    ref.addEventListener('paste', ref.pasteListener);
    document.addEventListener('selectionchange', ref.selectionChangeListener);
    document.execCommand('styleWithCSS', false, true);
  },
  saveSelection: function (ref) {
    if (document.activeElement == ref) {
      var selection = getSelection();
      if (selection.rangeCount > 0) {
        ref.range = selection.getRangeAt(0);
      }
    }
  },
  restoreSelection: function (ref) {
    var range = ref.range;
    if (range) {
      delete ref.range;
      ref.focus();
      var selection = getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
    }
  },
  selectionAttributes: function (selector, attributes) {
    var selection = getSelection();
    var target = selection.focusNode;
    var innerHTML;
    if (target) {
      if (target.nodeType == 3) {
        target = target.parentElement;
      } else {
        target = target.childNodes[selection.focusOffset];
        if (target) {
          innerHTML = target.outerHTML;
        }
      }
      if (target && !target.matches(selector)) {
        target = target.closest(selector);
      }
    }
    return attributes.reduce(function (result, name) {
      if (target) {
        result[name] = target[name];
      }
      return result;
    }, { innerText: selection.toString(), innerHTML: innerHTML });
  },
  destroyEditor: function (ref) {
    if (ref) {
      ref.removeEventListener('input', ref.inputListener);
      ref.removeEventListener('paste', ref.pasteListener);
      document.removeEventListener('selectionchange', ref.selectionChangeListener);
    }
  },
  startDrag: function (ref, instance, handler) {
    ref.mouseMoveHandler = function (e) {
      instance.invokeMethodAsync(handler, { clientX: e.clientX, clientY: e.clientY });
    };
    ref.touchMoveHandler = function (e) {
      if (e.targetTouches[0]) {
        instance.invokeMethodAsync(handler, { clientX: e.targetTouches[0].clientX, clientY: e.targetTouches[0].clientY });
      }
    };
    ref.mouseUpHandler = function (e) {
      Radzen.endDrag(ref);
    };
    document.addEventListener('mousemove', ref.mouseMoveHandler);
    document.addEventListener('mouseup', ref.mouseUpHandler);
    document.addEventListener('touchmove', ref.touchMoveHandler, { passive: true })
    document.addEventListener('touchend', ref.mouseUpHandler, { passive: true });
    return Radzen.clientRect(ref);
  },
  submit: function (form) {
    form.submit();
  },
  clientRect: function (arg) {
    var el = arg instanceof Element || arg instanceof HTMLDocument
        ? arg
        : document.getElementById(arg);
    var rect = el.getBoundingClientRect();
    return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
  },
  endDrag: function (ref) {
    document.removeEventListener('mousemove', ref.mouseMoveHandler);
    document.removeEventListener('mouseup', ref.mouseUpHandler);
    document.removeEventListener('touchmove', ref.touchMoveHandler)
    document.removeEventListener('touchend', ref.mouseUpHandler);
  },
  startColumnReorder: function(id) {
      var el = document.getElementById(id);
      var cell = el.parentNode.parentNode;
      var visual = document.createElement("th");
      visual.className = cell.className + ' rz-column-draggable';
      visual.style = cell.style;
      visual.style.display = 'none';
      visual.style.position = 'absolute';
      visual.style.height = cell.offsetHeight + 'px';
      visual.style.width = cell.offsetWidth + 'px';
      visual.style.zIndex = 2000;
      visual.innerHTML = cell.innerHTML;
      visual.id = id + 'visual';
      document.body.appendChild(visual);

      Radzen[id + 'end'] = function (e) {
          var el = document.getElementById(id + 'visual');
          if (el) {
              document.body.removeChild(el);
              Radzen[id + 'end'] = null;
              Radzen[id + 'move'] = null;
          }
      }
      document.removeEventListener('click', Radzen[id + 'end']);
      document.addEventListener('click', Radzen[id + 'end']);

      Radzen[id + 'move'] = function (e) {
          var el = document.getElementById(id + 'visual');
          if (el) {
              el.style.display = 'block';

              if (/Edge/.test(navigator.userAgent)) {
                  var scrollLeft = document.body.scrollLeft;
                  var scrollTop = document.body.scrollTop;
              } else {
                  var scrollLeft = document.documentElement.scrollLeft;
                  var scrollTop = document.documentElement.scrollTop;
              }

              el.style.top = e.clientY + scrollTop + 10 + 'px';
              el.style.left = e.clientX + scrollLeft + 10 + 'px';
          }
      }
      document.removeEventListener('mousemove', Radzen[id + 'move']);
      document.addEventListener('mousemove', Radzen[id + 'move']);
  },
  startColumnResize: function(id, grid, columnIndex, clientX) {
      var el = document.getElementById(id);
      var cell = el.parentNode.parentNode;
      var col = document.getElementById(id + '-col');
      var dataCol = document.getElementById(id + '-data-col');
      var footerCol = document.getElementById(id + '-footer-col');
      Radzen[el] = {
          clientX: clientX,
          width: cell.getBoundingClientRect().width,
          mouseUpHandler: function (e) {
              if (Radzen[el]) {
                  grid.invokeMethodAsync(
                      'RadzenGrid.OnColumnResized',
                      columnIndex,
                      cell.getBoundingClientRect().width
                  );
                  document.removeEventListener('mousemove', Radzen[el].mouseMoveHandler);
                  document.removeEventListener('mouseup', Radzen[el].mouseUpHandler);
                  document.removeEventListener('touchmove', Radzen[el].touchMoveHandler)
                  document.removeEventListener('touchend', Radzen[el].mouseUpHandler);
                  Radzen[el] = null;
              }
          },
          mouseMoveHandler: function (e) {
              if (Radzen[el]) {
                  var width = (Radzen[el].width - (Radzen[el].clientX - e.clientX)) + 'px';
                  if (cell) {
                      cell.style.width = width;
                  }
                  if (col) {
                      col.style.width = width;
                  }
                  if (dataCol) {
                      dataCol.style.width = width;
                  }
                  if (footerCol) {
                      footerCol.style.width = width;
                  }
              }
          },
          touchMoveHandler: function (e) {
              if (e.targetTouches[0]) {
                  Radzen[el].mouseMoveHandler(e.targetTouches[0]);
              }
          }
      };
      document.addEventListener('mousemove', Radzen[el].mouseMoveHandler);
      document.addEventListener('mouseup', Radzen[el].mouseUpHandler);
      document.addEventListener('touchmove', Radzen[el].touchMoveHandler, { passive: true })
      document.addEventListener('touchend', Radzen[el].mouseUpHandler, { passive: true });
  },
      startSplitterResize: function(id,
        splitter,
        paneId,
        paneNextId,
        orientation,
        clientPos,
        minValue,
        maxValue,
        minNextValue,
        maxNextValue) {

        var el = document.getElementById(id);
        var pane = document.getElementById(paneId);
        var paneNext = document.getElementById(paneNextId);
        var paneLength;
        var paneNextLength;
        var panePerc;
        var paneNextPerc;
        var isHOrientation=orientation == 'Horizontal';

        var totalLength = 0.0;
        Array.from(el.children).forEach(element => {
            totalLength += isHOrientation
                ? element.getBoundingClientRect().width
                : element.getBoundingClientRect().height;
        });

        if (pane) {
            paneLength = isHOrientation
                ? pane.getBoundingClientRect().width
                : pane.getBoundingClientRect().height;

            panePerc = (paneLength / totalLength * 100) + '%';
        }

        if (paneNext) {
            paneNextLength = isHOrientation
                ? paneNext.getBoundingClientRect().width
                : paneNext.getBoundingClientRect().height;

            paneNextPerc = (paneNextLength / totalLength * 100) + '%';
        }

        function ensurevalue(value) {
            if (!value)
                return null;

            value=value.trim().toLowerCase();

            if (value.endsWith("%"))
                return totalLength*parseFloat(value)/100;
            
            if (value.endsWith("px"))
                return parseFloat(value);

            throw 'Invalid value';
        }

        minValue=ensurevalue(minValue);
        maxValue=ensurevalue(maxValue);
        minNextValue=ensurevalue(minNextValue);
        maxNextValue=ensurevalue(maxNextValue);
        
        Radzen[el] = {
            clientPos: clientPos,
            panePerc: parseFloat(panePerc),
            paneNextPerc: isFinite(parseFloat(paneNextPerc)) ? parseFloat(paneNextPerc) : 0,
            paneLength: paneLength,
            paneNextLength: isFinite(paneNextLength) ? paneNextLength : 0,
            mouseUpHandler: function(e) {
                if (Radzen[el]) {
                    splitter.invokeMethodAsync(
                        'RadzenSplitter.OnPaneResized',
                        parseInt(pane.getAttribute('data-index')),
                        parseFloat(pane.style.flexBasis),
                        paneNext ? parseInt(paneNext.getAttribute('data-index')) : null,
                        paneNext ? parseFloat(paneNext.style.flexBasis) : null
                    );
                    document.removeEventListener('mousemove', Radzen[el].mouseMoveHandler);
                    document.removeEventListener('mouseup', Radzen[el].mouseUpHandler);
                    document.removeEventListener('touchmove', Radzen[el].touchMoveHandler);
                    document.removeEventListener('touchend', Radzen[el].mouseUpHandler);
                    Radzen[el] = null;
                }
            },
            mouseMoveHandler: function(e) {
                if (Radzen[el]) {

                    var spacePerc = Radzen[el].panePerc + Radzen[el].paneNextPerc;
                    var spaceLength = Radzen[el].paneLength + Radzen[el].paneNextLength;

                    var length = (Radzen[el].paneLength -
                        (Radzen[el].clientPos - (isHOrientation ? e.clientX : e.clientY)));
                    
                    if (length > spaceLength)
                        length = spaceLength;

                    if (minValue && length < minValue) length = minValue;
                    if (maxValue && length > maxValue) length = maxValue;

                    if (paneNext) {
                        var nextSpace=spaceLength-length;
                        if (minNextValue && nextSpace < minNextValue) length = spaceLength-minNextValue;
                        if (maxNextValue && nextSpace > maxNextValue) length = spaceLength+maxNextValue;
                    }
                    
                    var perc = length / Radzen[el].paneLength;
                    if (!isFinite(perc)) {
                        perc = 1;
                        Radzen[el].panePerc = 0.1;
                        Radzen[el].paneLength =isHOrientation
                            ? pane.getBoundingClientRect().width
                            : pane.getBoundingClientRect().height;
                    }
                    
                    var newPerc =  Radzen[el].panePerc * perc;
                    if (newPerc < 0) newPerc = 0;
                    if (newPerc > 100) newPerc = 100;
                    
                    pane.style.flexBasis = newPerc + '%';
                    if (paneNext)
                        paneNext.style.flexBasis = (spacePerc - newPerc) + '%';
                }
            },
            touchMoveHandler: function(e) {
                if (e.targetTouches[0]) {
                    Radzen[el].mouseMoveHandler(e.targetTouches[0]);
                }
            }
        };
        document.addEventListener('mousemove', Radzen[el].mouseMoveHandler);
        document.addEventListener('mouseup', Radzen[el].mouseUpHandler);
        document.addEventListener('touchmove', Radzen[el].touchMoveHandler, { passive: true });
        document.addEventListener('touchend', Radzen[el].mouseUpHandler, { passive: true });
    }
};
