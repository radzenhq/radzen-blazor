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
var radzenRecognition;

window.Radzen = {
    isRTL: function (el) {
        return el && getComputedStyle(el).direction == 'rtl';
    },
    throttle: function (callback, delay) {
        var timeout = null;
        return function () {
            var args = arguments;
            var ctx = this;
            if (!timeout) {
                timeout = setTimeout(function () {
                    callback.apply(ctx, args);
                    timeout = null;
                }, delay);
            }
        };
    },
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
                      if (c === '*' || c == chars[count]) {
                          formatted += chars[count];
                          count++;
                      } else {
                          formatted += c;
                      }
                  }
              }
              return formatted;
          }

          if (window.safari !== undefined) {
              el.onblur = function (e) {
                  el.dispatchEvent(new Event('change'));
              };
          }

          var start = el.selectionStart != el.value.length ? el.selectionStart : -1;
          var end = el.selectionEnd != el.value.length ? el.selectionEnd : -1;

          el.value = format(el.value, mask, pattern, characterPattern);

          el.selectionStart = start != -1 ? start : el.selectionStart;
          el.selectionEnd = end != -1 ? end : el.selectionEnd;
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
                content.style.display = i == index ? '' : 'none';
            }
            var header = el.parentNode.previousElementSibling.children[i];
            if (header) {
                if (i == index) {
                    header.classList.add('rz-tabview-selected');
                    header.classList.add('rz-state-focused');
                }
                else {
                    header.classList.remove('rz-tabview-selected');
                    header.classList.remove('rz-state-focused');
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
      'callback=rz_map_init&libraries=marker';

    script.async = true;
    script.defer = true;
    script.onerror = function (err) {
      for (var i = 0; i < rejectCallbacks.length; i++) {
        rejectCallbacks[i](err);
      }
    };

    document.body.appendChild(script);
  },
  createMap: function (wrapper, ref, id, apiKey, mapId, zoom, center, markers, options, fitBoundsToMarkersOnUpdate) {
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
        zoom: zoom,
        mapId: mapId
      });

      Radzen[id].instance.addListener('click', function (e) {
        Radzen[id].invokeMethodAsync('RadzenGoogleMap.OnMapClick', {
          Position: {Lat: e.latLng.lat(), Lng: e.latLng.lng()}
        });
      });

      Radzen.updateMap(id, zoom, center, markers, options, fitBoundsToMarkersOnUpdate);
    });
  },
  updateMap: function (id, zoom, center, markers, options, fitBoundsToMarkersOnUpdate) {
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
        let markerBounds = new google.maps.LatLngBounds();

        if (Radzen[id] && Radzen[id].instance) {
            if (Radzen[id].instance.markers && Radzen[id].instance.markers.length) {
                for (var i = 0; i < Radzen[id].instance.markers.length; i++) {
                    Radzen[id].instance.markers[i].setMap(null);
                }
            }

            if (markers) {
                Radzen[id].instance.markers = [];

                markers.forEach(function (m) {
                    var content;

                    if (m.label) {
                        content = document.createElement('span');
                        content.innerHTML = m.label;
                    }

                    var marker = new this.google.maps.marker.AdvancedMarkerElement({
                        position: m.position,
                        title: m.title,
                        content: content
                    });

                    marker.addListener('click', function (e) {
                        Radzen[id].invokeMethodAsync('RadzenGoogleMap.OnMarkerClick', {
                            Title: marker.title,
                            Label: marker.content.innerText,
                            Position: marker.position
                        });
                    });

                    marker.setMap(Radzen[id].instance);

                    Radzen[id].instance.markers.push(marker);

                        markerBounds.extend(marker.position);
                });
            }

            if (zoom) {
                Radzen[id].instance.setZoom(zoom);
                }

            if (center) {
                Radzen[id].instance.setCenter(center);
            }

            if (options) {
                Radzen[id].instance.setOptions(options);
            }

            if (markers && fitBoundsToMarkersOnUpdate) {
                Radzen[id].instance.fitBounds(markerBounds);
            }
        }
    });
  },
  destroyMap: function (id) {
    if (Radzen[id].instance) {
      delete Radzen[id].instance;
    }
  },
 focusSecurityCode: function (el) {
    if (!el) return;
    var firstInput = el.querySelector('.rz-security-code-input');
    if (firstInput) {
        setTimeout(function () { firstInput.focus() }, 500);
    }
 },
  destroySecurityCode: function (id, el) {
    if (!Radzen[id]) return;

    var inputs = el.getElementsByTagName('input');

    if (Radzen[id].keyPress && Radzen[id].paste) {
        for (var i = 0; i < inputs.length; i++) {
            inputs[i].removeEventListener('keypress', Radzen[id].keyPress);
            inputs[i].removeEventListener('keydown', Radzen[id].keyDown);
            inputs[i].removeEventListener('paste', Radzen[id].paste);
        }
        delete Radzen[id].keyPress;
        delete Radzen[id].paste;
    }

    Radzen[id] = null;
  },
  createSecurityCode: function (id, ref, el, isNumber) {
      if (!el || !ref) return;

      var hidden = el.querySelector('input[type="hidden"]');

      Radzen[id] = {};

      Radzen[id].inputs = [...el.querySelectorAll('.rz-security-code-input')];

      Radzen[id].paste = function (e) {
          if (e.clipboardData) {
              var value = e.clipboardData.getData('text');

              if (value) {
                  for (var i = 0; i < value.length; i++) {
                      if (isNumber && isNaN(+value[i])) {
                          continue;
                      }
                      Radzen[id].inputs[i].value = value[i];
                  }

                  var code = Radzen[id].inputs.map(i => i.value).join('').trim();
                  hidden.value = code;

                  ref.invokeMethodAsync('RadzenSecurityCode.OnValueChange', code);

                  Radzen[id].inputs[Radzen[id].inputs.length - 1].focus();
              }

              e.preventDefault();
          }
      }
      Radzen[id].keyPress = function (e) {
          var keyCode = e.data ? e.data.charCodeAt(0) : e.which;
          var ch = e.data || String.fromCharCode(e.which);

          if (e.metaKey ||
              e.ctrlKey ||
              keyCode == 9 ||
              keyCode == 8 ||
              keyCode == 13
          ) {
              return;
          }

          if (isNumber && (keyCode < 48 || keyCode > 57)) {
              e.preventDefault();
              return;
          }

          if (e.currentTarget.value == ch) {
              return;
          }

          e.currentTarget.value = ch;

          var value = Radzen[id].inputs.map(i => i.value).join('').trim();
          hidden.value = value;

          ref.invokeMethodAsync('RadzenSecurityCode.OnValueChange', value);

          var index = Radzen[id].inputs.indexOf(e.currentTarget);
          if (index < Radzen[id].inputs.length - 1) {
              Radzen[id].inputs[index + 1].focus();
          }
      }

      Radzen[id].keyDown = function (e) {
          var keyCode = e.data ? e.data.charCodeAt(0) : e.which;
          if (keyCode == 8) {
              e.currentTarget.value = '';

              var value = Radzen[id].inputs.map(i => i.value).join('').trim();
              hidden.value = value;

              ref.invokeMethodAsync('RadzenSecurityCode.OnValueChange', value);

              var index = Radzen[id].inputs.indexOf(e.currentTarget);
              if (index > 0) {
                  Radzen[id].inputs[index - 1].focus();
              }
          }
      }

      for (var i = 0; i < Radzen[id].inputs.length; i++) {
          Radzen[id].inputs[i].addEventListener(navigator.userAgent.match(/Android/i) ? 'textInput' : 'keypress', Radzen[id].keyPress);
          Radzen[id].inputs[i].addEventListener(navigator.userAgent.match(/Android/i) ? 'textInput' : 'keydown', Radzen[id].keyDown);
          Radzen[id].inputs[i].addEventListener('paste', Radzen[id].paste);
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
      var percent = (Radzen.isRTL(handle) ? parent.offsetWidth - handle.offsetLeft - offsetX
            : handle.offsetLeft + offsetX) / parent.offsetWidth;

      if (percent > 1) {
          percent = 1;
      } else if (percent < 0) {
          percent = 0;
      }

      var newValue = percent * (max - min) + min;

      if (
        slider.canChange &&
        newValue >= min &&
        newValue <= max
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
        var newValue = percent * (max - min) + min;
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
  prepareDrag: function (el) {
    if (el) {
        el.ondragover = function (e) { e.preventDefault(); };
        el.ondragstart = function (e) { e.dataTransfer.setData('', e.target.id); };
    }
  },
  focusElement: function (elementId) {
    var el = document.getElementById(elementId);
    if (el) {
      el.focus();
    }
  },
  scrollIntoViewIfNeeded: function (ref, selector) {
    var el = selector ? ref.getElementsByClassName(selector)[0] : ref;
    if (el && el.scrollIntoViewIfNeeded) {
        el.scrollIntoViewIfNeeded();
    } else if (el && el.scrollIntoView) {
        el.scrollIntoView();
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
      childNodes[ul.nextSelectedIndex].scrollIntoView({block:'nearest'});
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
        while (ul.nextSelectedIndex < childNodes.length - 1) {
            ul.nextSelectedIndex++;
            if (!childNodes[ul.nextSelectedIndex].classList.contains('rz-state-disabled'))
                break;
        }
    } else {
        while (ul.nextSelectedIndex > 0) {
            ul.nextSelectedIndex--;
            if (!childNodes[ul.nextSelectedIndex].classList.contains('rz-state-disabled'))
                break;
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
      Radzen.scrollIntoViewIfNeeded(childNodes[ul.nextSelectedIndex]);
    }

    return ul.nextSelectedIndex;
  },
  clearFocusedHeaderCell: function (gridId) {
    var grid = document.getElementById(gridId);
    if (!grid) return;

    var table = grid.querySelector('.rz-grid-table');
    var thead = table.getElementsByTagName("thead")[0];
    var highlightedCells = thead.querySelectorAll('.rz-state-focused');
    if (highlightedCells.length) {
        for (var i = 0; i < highlightedCells.length; i++) {
            highlightedCells[i].classList.remove('rz-state-focused');
        }
    }
  },
  focusTableRow: function (gridId, key, rowIndex, cellIndex, isVirtual) {
    var grid = document.getElementById(gridId);
    if (!grid) return;

    var table = grid.querySelector('.rz-grid-table');
    var tbody = table.tBodies[0];
    var thead = table.tHead;

    var rows = (cellIndex != null && thead && thead.rows && thead.rows.length ? [...thead.rows] : []).concat(tbody && tbody.rows && tbody.rows.length ? [...tbody.rows] : []);

    if (isVirtual && (key == 'ArrowUp' || key == 'ArrowDown' || key == 'PageUp' || key == 'PageDown' || key == 'Home' || key == 'End')) {
        if (rowIndex == 0 && (key == 'End' || key == 'PageDown')) {
            var highlightedCells = thead.querySelectorAll('.rz-state-focused');
            if (highlightedCells.length) {
                for (var i = 0; i < highlightedCells.length; i++) {
                    highlightedCells[i].classList.remove('rz-state-focused');
                }
            }
        }
        if (key == 'ArrowUp' || key == 'ArrowDown' || key == 'PageUp' || key == 'PageDown') {
            var rowHeight = rows[rows.length - 1] ? rows[rows.length - 1].offsetHeight : 40;
            var factor = key == 'PageUp' || key == 'PageDown' ? 10 : 1;
            table.parentNode.scrollTop = table.parentNode.scrollTop + (factor * (key == 'ArrowDown' || key == 'PageDown' ? rowHeight : -rowHeight));
        }
        else {
            table.parentNode.scrollTop = key == 'Home' ? 0 : table.parentNode.scrollHeight;
        }
    }

    table.nextSelectedIndex = rowIndex || 0;
    table.nextSelectedCellIndex = cellIndex || 0;

    if (key == 'ArrowDown') {
        while (table.nextSelectedIndex < rows.length - 1) {
            table.nextSelectedIndex++;
            if (!rows[table.nextSelectedIndex].classList.contains('rz-state-disabled'))
                break;
        }
    } else if (key == 'ArrowUp') {
        while (table.nextSelectedIndex > 0) {
            table.nextSelectedIndex--;
            if (!rows[table.nextSelectedIndex].classList.contains('rz-state-disabled'))
                break;
        }
    } else if (key == 'ArrowRight') {
        while (table.nextSelectedCellIndex < rows[table.nextSelectedIndex].cells.length - 1) {
            table.nextSelectedCellIndex++;
            if (!rows[table.nextSelectedIndex].cells[table.nextSelectedCellIndex].classList.contains('rz-state-disabled'))
                break;
        }
    } else if (key == 'ArrowLeft') {
        while (table.nextSelectedCellIndex > 0) {
            table.nextSelectedCellIndex--;
            if (!rows[table.nextSelectedIndex].cells[table.nextSelectedCellIndex].classList.contains('rz-state-disabled'))
                break;
        }
    } else if (isVirtual && (key == 'PageDown' || key == 'End')) {
        table.nextSelectedIndex = rows.length - 1;
    } else if (isVirtual && (key == 'PageUp' || key == 'Home')) {
        table.nextSelectedIndex = 1;
    }

    if (key == 'ArrowLeft' || key == 'ArrowRight' || (key == 'ArrowUp' && table.nextSelectedIndex == 0 && table.parentNode.scrollTop == 0)) {
        var highlightedCells = rows[table.nextSelectedIndex].querySelectorAll('.rz-state-focused');
        if (highlightedCells.length) {
            for (var i = 0; i < highlightedCells.length; i++) {
                highlightedCells[i].classList.remove('rz-state-focused');
            }
        }

        if (
            table.nextSelectedCellIndex >= 0 &&
            table.nextSelectedCellIndex <= rows[table.nextSelectedIndex].cells.length - 1
        ) {
            var cell = rows[table.nextSelectedIndex].cells[table.nextSelectedCellIndex];

            if (!cell.classList.contains('rz-state-focused')) {
                cell.classList.add('rz-state-focused');
                if (!isVirtual && table.parentElement.scrollWidth > table.parentElement.clientWidth) {
                    Radzen.scrollIntoViewIfNeeded(cell);
                }
            }
        }
    } else if (key == 'ArrowDown' || key == 'ArrowUp') {
        var highlighted = table.querySelectorAll('.rz-state-focused');
        if (highlighted.length) {
            for (var i = 0; i < highlighted.length; i++) {
                highlighted[i].classList.remove('rz-state-focused');
            }
        }

        if (table.nextSelectedIndex >= 0 &&
            table.nextSelectedIndex <= rows.length - 1
        ) {
            var row = rows[table.nextSelectedIndex];

            if (!row.classList.contains('rz-state-focused')) {
                row.classList.add('rz-state-focused');
                if (!isVirtual && table.parentElement.scrollHeight > table.parentElement.clientHeight) {
                    Radzen.scrollIntoViewIfNeeded(row);
                }
            }
        }
    }

    return [table.nextSelectedIndex, table.nextSelectedCellIndex];
  },
  uploadInputChange: function (e, url, auto, multiple, clear, parameterName) {
      if (auto) {
          Radzen.upload(e.target, url, multiple, clear, parameterName);
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
  removeFileFromFileInput: function (fileInput) {
    fileInput.value = '';
  },
  upload: function (fileInput, url, multiple, clear, parameterName) {
    var uploadComponent = Radzen.uploadComponents && Radzen.uploadComponents[fileInput.id];
    if (!uploadComponent) { return; }
    if (!uploadComponent.files || clear) {
        uploadComponent.files = Array.from(fileInput.files);
    }
    var data = new FormData();
    var files = [];
    var cancelled = false;
    for (var i = 0; i < uploadComponent.files.length; i++) {
      var file = uploadComponent.files[i];
      data.append(parameterName || (multiple ? 'files' : 'file'), file, file.name);
      files.push({Name: file.name, Size: file.size});
    }
    var xhr = new XMLHttpRequest();
    xhr.withCredentials = true;
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
            files,
            cancelled
          ).then(function (cancel) {
              if (cancel) {
                  cancelled = true;
                  xhr.abort();
              }
          });
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
                xhr.responseText,
                cancelled
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
  numericOnPaste: function (e, min, max) {
    if (e.clipboardData) {
      var value = e.clipboardData.getData('text');

      if (value && !isNaN(+value)) {
        var numericValue = +value;
        if (min != null && numericValue >= min) {
            return;
        }
        if (max != null && numericValue <= max) {
            return;
        }
      }

      e.preventDefault();
    }
  },
  numericOnInput: function (e, min, max, isNullable) {
      var value = e.target.value;

      if (!isNullable && value == '' && min != null) {
          e.target.value = min;
      }

      if (value && !isNaN(+value)) {
        var numericValue = +value;
        if (min != null && !isNaN(+min) && numericValue < min) {
            e.target.value = min;
        }
        if (max != null && !isNaN(+max) && numericValue > max) {
            e.target.value = max;
        }
      }
  },
  numericKeyPress: function (e, isInteger, decimalSeparator) {
    if (
      e.metaKey ||
      e.ctrlKey ||
      e.keyCode == 9 ||
      e.keyCode == 8 ||
      e.keyCode == 13
    ) {
      return;
      }

      if (e.code === 'NumpadDecimal') {
          var cursorPosition = e.target.selectionEnd;
          e.target.value = [e.target.value.slice(0, e.target.selectionStart), decimalSeparator, e.target.value.slice(e.target.selectionEnd)].join('');
          e.target.selectionStart = ++cursorPosition;
          e.target.selectionEnd = cursorPosition;
          e.preventDefault();
          return;
    }

    var ch = String.fromCharCode(e.charCode);

    if ((isInteger ? /^[-\d]$/ : /^[-\d,.]$/).test(ch)) {
      return;
    }

    e.preventDefault();
  },
  openContextMenu: function (x,y,id, instance, callback) {
    Radzen.closePopup(id);

    Radzen.openPopup(null, id, false, null, x, y, instance, callback);

    setTimeout(function () {
        var popup = document.getElementById(id);
        if (popup) {
            var menu = popup.querySelector('.rz-menu');
            if (menu) {
                menu.focus();
            }
        }
    }, 500);
  },
  openTooltip: function (target, id, delay, duration, position, closeTooltipOnDocumentClick, instance, callback) {
    Radzen.closeTooltip(id);

    if (delay) {
        Radzen[id + 'delay'] = setTimeout(Radzen.openPopup, delay, target, id, false, position, null, null, instance, callback, closeTooltipOnDocumentClick);
    } else {
        Radzen.openPopup(target, id, false, position, null, null, instance, callback, closeTooltipOnDocumentClick);
    }

    if (duration) {
      Radzen[id + 'duration'] = setTimeout(Radzen.closePopup, duration, id, instance, callback);
    }
  },
  closeTooltip(id) {
    Radzen.closePopup(id);

    if (Radzen[id + 'delay']) {
        clearTimeout(Radzen[id + 'delay']);
    }

    if (Radzen[id + 'duration']) {
        clearTimeout(Radzen[id + 'duration']);
    }
  },
  destroyDatePicker(id) {
      var el = document.getElementById(id);
      if (!el) return;

      var button = el.querySelector('.rz-datepicker-trigger');
      if (button) {
          button.onclick = null;
      }
      var input = el.querySelector('.rz-inputtext');
      if (input) {
          input.onclick = null;
      }
  },
  createDatePicker(el, popupId) {
      if(!el) return;
      var handler = function (e, condition) {
          if (condition) {
              Radzen.togglePopup(e.currentTarget.parentNode, popupId, false, null, null, true, false);
          }
      };

      var button = el.querySelector('.rz-datepicker-trigger');
      if (button) {
          button.onclick = function (e) {
              handler(e, !e.currentTarget.classList.contains('rz-state-disabled'));
          };
      }
      var input = el.querySelector('.rz-inputtext');
      if (input) {
          input.onclick = function (e) {
              handler(e, e.currentTarget.classList.contains('rz-readonly') || e.currentTarget.classList.contains('rz-input-trigger') );
          };
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

      if (top + rect.height > window.innerHeight + scrollTop && parentRect.top > rect.height) {
          top = parentRect.top - rect.height + scrollTop;
      }

      popup.style.top = top + 'px';
  },
  openPopup: function (parent, id, syncWidth, position, x, y, instance, callback, closeOnDocumentClick = true, autoFocusFirstElement = false, disableSmartPosition = false) {
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

    if (window.chrome) {
        var closestFrozenCell = popup.closest('.rz-frozen-cell');
        if (closestFrozenCell) {
            Radzen[id + 'FZL'] = { cell: closestFrozenCell, left: closestFrozenCell.style.left };
            closestFrozenCell.style.left = '';
        }
    }

    popup.style.display = 'block';

    var rect = popup.getBoundingClientRect();

    var smartPosition = !position || position == 'bottom';

    if (smartPosition && top + rect.height > window.innerHeight && parentRect.top > rect.height) {
        if (disableSmartPosition !== true) {
            top = parentRect.top - rect.height;
        }

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
        var lastPopup = Radzen.popups && Radzen.popups[Radzen.popups.length - 1];
        var currentPopup = lastPopup != null && document.getElementById(lastPopup.id) || popup;

        if (lastPopup) {
            currentPopup.instance = lastPopup.instance;
            currentPopup.callback = lastPopup.callback;
            currentPopup.parent = lastPopup.parent;
        }

        if(e.type == 'contextmenu' || !e.target || !closeOnDocumentClick) return;
        if (!/Android/i.test(navigator.userAgent) &&
            !['input', 'textarea'].includes(document.activeElement ? document.activeElement.tagName.toLowerCase() : '') && e.type == 'resize') {
            Radzen.closePopup(currentPopup.id, currentPopup.instance, currentPopup.callback, e);
            return;
        }

        var closestLink = e.target.closest && (e.target.closest('.rz-link') || e.target.closest('.rz-navigation-item-link'));
        if (closestLink && closestLink.closest && closestLink.closest('a')) {
            if (Radzen.closeAllPopups) {
                Radzen.closeAllPopups();
            }
        }
        if (currentPopup.parent) {
          if (e.type == 'mousedown' && !currentPopup.parent.contains(e.target) && !currentPopup.contains(e.target)) {
              Radzen.closePopup(currentPopup.id, currentPopup.instance, currentPopup.callback, e);
          }
        } else {
          if (!currentPopup.contains(e.target)) {
              Radzen.closePopup(currentPopup.id, currentPopup.instance, currentPopup.callback, e);
          }
        }
    };

    if (!Radzen.popups) {
        Radzen.popups = [];
    }

    Radzen.popups.push({ id, instance, callback, parent });

    document.body.appendChild(popup);
    document.removeEventListener('mousedown', Radzen[id]);
    document.addEventListener('mousedown', Radzen[id]);
    window.removeEventListener('resize', Radzen[id]);
    window.addEventListener('resize', Radzen[id]);

    var p = parent;
    while (p && p != document.body) {
        if (p.scrollWidth > p.clientWidth || p.scrollHeight > p.clientHeight) {
            p.removeEventListener('scroll', Radzen.closeAllPopups);
            p.addEventListener('scroll', Radzen.closeAllPopups);
        }
        p = p.parentElement;
    }

    if (!parent) {
        document.removeEventListener('contextmenu', Radzen[id]);
        document.addEventListener('contextmenu', Radzen[id]);
    }

    if (autoFocusFirstElement) {
        setTimeout(function () {
            popup.removeEventListener('keydown', Radzen.focusTrap);
            popup.addEventListener('keydown', Radzen.focusTrap);

            var focusable = Radzen.getFocusableElements(popup);
            var firstFocusable = focusable[0];
            if (firstFocusable) {
                firstFocusable.focus();
            }
        }, 500);
    }
  },
  closeAllPopups: function (e, id) {
    if (!Radzen.popups) return;
    var el = e && e.target || id && documentElement.getElementById(id);
    var popups = Radzen.popups;
      for (var i = 0; i < popups.length; i++) {
        var p = popups[i];

        var closestPopup = el && el.closest && (el.closest('.rz-popup') || el.closest('.rz-overlaypanel'));
        if (closestPopup && closestPopup != p) {
            return;
        }

        Radzen.closePopup(p.id, p.instance, p.callback, e);
    }
    Radzen.popups = [];
  },
  closePopup: function (id, instance, callback, e) {
    var popup = document.getElementById(id);
    if (!popup) return;
    if (popup.style.display == 'none') {
        var popups = Radzen.findPopup(id);
        if (popups.length > 1) {
            for (var i = 0; i < popups.length; i++) {
                if (popups[i].style.display == 'none') {
                    popups[i].parentNode.removeChild(popups[i]);
                } else {
                    popup = popups[i];
                }
            }
        } else {
            return;
        }
    }

    if (popup) {
      if (popup.minWidth) {
          popup.style.minWidth = '';
      }

      if (window.chrome && Radzen[id + 'FZL']) {
        Radzen[id + 'FZL'].cell.style.left = Radzen[id + 'FZL'].left;
        Radzen[id + 'FZL'] = null;
      }

      popup.style.display = 'none';
    }
    document.removeEventListener('mousedown', Radzen[id]);
    window.removeEventListener('resize', Radzen[id]);
    Radzen[id] = null;

    if (instance) {
      instance.invokeMethodAsync(callback);
    }
    Radzen.popups = (Radzen.popups || []).filter(function (obj) {
        return obj.id !== id;
    });

    if (Radzen.activeElement && Radzen.activeElement == document.activeElement ||
        Radzen.activeElement && document.activeElement == document.body ||
        Radzen.activeElement && document.activeElement &&
            (document.activeElement.classList.contains('rz-dropdown-filter') || document.activeElement.classList.contains('rz-lookup-search-input'))) {
        setTimeout(function () {
            if (e && e.target && e.target.tabIndex != -1) {
                Radzen.activeElement = e.target;
            }
            if (Radzen.activeElement) {
               Radzen.activeElement.focus();
            }
            Radzen.activeElement = null;
        }, 100);
    }
  },
  popupOpened: function (id) {
    var popup = document.getElementById(id);
    if (popup) {
        return popup.style.display != 'none';
    }
    return false;
  },
  togglePopup: function (parent, id, syncWidth, instance, callback, closeOnDocumentClick = true, autoFocusFirstElement = false) {
    var popup = document.getElementById(id);
    if (!popup) return;
    if (popup.style.display == 'block') {
      Radzen.closePopup(id, instance, callback);
    } else {
      Radzen.openPopup(parent, id, syncWidth, null, null, null, instance, callback, closeOnDocumentClick, autoFocusFirstElement);
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
  focusFirstFocusableElement: function (el) {
      var focusable = Radzen.getFocusableElements(el);
      var editor = el.querySelector('.rz-html-editor');

      if (editor && !focusable.includes(editor.previousElementSibling)) {
          var editable = el.querySelector('.rz-html-editor-content');
          if (editable) {
              var selection = window.getSelection();
              var range = document.createRange();
              range.setStart(editable, 0);
              range.setEnd(editable, 0);
              selection.removeAllRanges();
              selection.addRange(range);
          }
      } else {
          var firstFocusable = focusable[0];
          if (firstFocusable) {
              firstFocusable.focus();
          }
      }
  },
  openSideDialog: function (options) {
      setTimeout(function () {
          if (options.autoFocusFirstElement) {
              var dialogs = document.querySelectorAll('.rz-dialog-side-content');
              if (dialogs.length == 0) return;
              var lastDialog = dialogs[dialogs.length - 1];
              Radzen.focusFirstFocusableElement(lastDialog);
          }
      }, 500);
  },
  openDialog: function (options, dialogService, dialog) {
    if (Radzen.closeAllPopups) {
        Radzen.closeAllPopups();
    }
    Radzen.dialogService = dialogService;
    if (
      document.documentElement.scrollHeight >
      document.documentElement.clientHeight
    ) {
      document.body.classList.add('no-scroll');
    }

    setTimeout(function () {
        var dialogs = document.querySelectorAll('.rz-dialog-content');
        if (dialogs.length == 0) return;
        var lastDialog = dialogs[dialogs.length - 1];

        if (lastDialog) {
            lastDialog.options = options;
            lastDialog.removeEventListener('keydown', Radzen.focusTrap);
            lastDialog.addEventListener('keydown', Radzen.focusTrap);

            if (options.resizable) {
                dialog.offsetWidth = lastDialog.parentElement.offsetWidth;
                dialog.offsetHeight = lastDialog.parentElement.offsetHeight;
                var dialogResize = function (e) {
                    if (!dialog) return;
                    if (dialog.offsetWidth != e[0].target.offsetWidth || dialog.offsetHeight != e[0].target.offsetHeight) {

                        dialog.offsetWidth = e[0].target.offsetWidth;
                        dialog.offsetHeight = e[0].target.offsetHeight;

                        dialog.invokeMethodAsync(
                            'RadzenDialog.OnResize',
                            e[0].target.offsetWidth,
                            e[0].target.offsetHeight
                        );
                    }
                };
                Radzen.dialogResizer = new ResizeObserver(dialogResize).observe(lastDialog.parentElement);
            }

            if (options.draggable) {
                var dialogTitle = lastDialog.parentElement.querySelector('.rz-dialog-titlebar');
                if (dialogTitle) {
                    Radzen[dialogTitle] = function (e) {
                        var rect = lastDialog.parentElement.getBoundingClientRect();
                        var offsetX = e.clientX - rect.left;
                        var offsetY = e.clientY - rect.top;

                        var move = function (e) {
                            var left = e.clientX - offsetX;
                            var top = e.clientY - offsetY;

                            lastDialog.parentElement.style.left = left + 'px';
                            lastDialog.parentElement.style.top = top + 'px';

                            dialog.invokeMethodAsync('RadzenDialog.OnDrag', top, left);
                        };

                        var stop = function () {
                            document.removeEventListener('mousemove', move);
                            document.removeEventListener('mouseup', stop);
                        };

                        document.addEventListener('mousemove', move);
                        document.addEventListener('mouseup', stop);
                    };

                    dialogTitle.addEventListener('mousedown', Radzen[dialogTitle]);
                }
            }

            if (options.autoFocusFirstElement) {
                Radzen.focusFirstFocusableElement(lastDialog);
            }
        }
    }, 500);

    document.removeEventListener('keydown', Radzen.closePopupOrDialog);
    if (options.closeDialogOnEsc) {
        document.addEventListener('keydown', Radzen.closePopupOrDialog);
    }
  },
  closeDialog: function () {
    Radzen.dialogResizer = null;
    document.body.classList.remove('no-scroll');
    var dialogs = document.querySelectorAll('.rz-dialog-content');

    var lastDialog = dialogs.length && dialogs[dialogs.length - 1];
    if (lastDialog) {
        var dialogTitle = lastDialog.parentElement.querySelector('.rz-dialog-titlebar');
        if (dialogTitle) {
            dialogTitle.removeEventListener('mousedown', Radzen[dialogTitle]);
            Radzen[dialogTitle] = null;
            delete Radzen[dialogTitle];
        }
    }

    if (dialogs.length <= 1) {
        document.removeEventListener('keydown', Radzen.closePopupOrDialog);
        delete Radzen.dialogService;
    }
  },
  disableKeydown: function (e) {
      e = e || window.event;
      e.preventDefault();
  },
  getFocusableElements: function (element) {
    return [...element.querySelectorAll('a, button, input, textarea, select, details, iframe, embed, object, summary dialog, audio[controls], video[controls], [contenteditable], [tabindex]')]
        .filter(el => el && el.tabIndex > -1 && !el.hasAttribute('disabled') && el.offsetParent !== null);
  },
  focusTrap: function (e) {
    e = e || window.event;
    var isTab = false;
    if ("key" in e) {
        isTab = (e.key === "Tab");
    } else {
        isTab = (e.keyCode === 9);
    }
    if (isTab) {
        var focusable = Radzen.getFocusableElements(e.currentTarget);
        var firstFocusable = focusable[0];
        var lastFocusable = focusable[focusable.length - 1];

        if (firstFocusable && lastFocusable && e.shiftKey && document.activeElement === firstFocusable) {
            e.preventDefault();
            lastFocusable.focus();
        } else if (firstFocusable && lastFocusable && !e.shiftKey && document.activeElement === lastFocusable) {
            e.preventDefault();
            firstFocusable.focus();
        }
    }
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

          var dialogs = document.querySelectorAll('.rz-dialog-content');
          if (dialogs.length == 0) return;
          var lastDialog = dialogs[dialogs.length - 1];

          if (lastDialog && lastDialog.options && lastDialog.options.closeDialogOnEsc) {
              Radzen.dialogService.invokeMethodAsync('DialogService.Close', null);

              if (dialogs.length <= 1) {
                  document.removeEventListener('keydown', Radzen.closePopupOrDialog);
                  delete Radzen.dialogService;
                  var layout = document.querySelector('.rz-layout');
                  if (layout) {
                      layout.removeEventListener('keydown', Radzen.disableKeydown);
                  }
              }
          }
      }
  },
  getInputValue: function (arg) {
    var input =
      arg instanceof Element || arg instanceof HTMLDocument
        ? arg
        : document.getElementById(arg);
    return input && input.value != '' ? input.value : null;
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
  toggleMenuItem: function (target, event, defaultActive, clickToOpen) {
    var item = target.closest('.rz-navigation-item');

    var active = defaultActive != undefined ? defaultActive : !item.classList.contains('rz-navigation-item-active');

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

    if (clickToOpen === false && item.parentElement && item.parentElement.closest('.rz-navigation-item') && !defaultActive) {
        return;
    };

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
    ref.removeEventListener('mouseleave', ref.mouseLeaveHandler);
    delete ref.mouseLeaveHandler;
    ref.removeEventListener('mouseenter', ref.mouseEnterHandler);
    delete ref.mouseEnterHandler;
    ref.removeEventListener('mousemove', ref.mouseMoveHandler);
    delete ref.mouseMoveHandler;
    ref.removeEventListener('click', ref.clickHandler);
    delete ref.clickHandler;
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
    var inside = false;
    ref.mouseMoveHandler = this.throttle(function (e) {
      if (inside) {
        if (e.target.matches('.rz-chart-tooltip-content') || e.target.closest('.rz-chart-tooltip-content')) {
            return
        }
        var rect = ref.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;
        instance.invokeMethodAsync('MouseMove', x, y);
     }
    }, 100);
    ref.mouseEnterHandler = function () {
        inside = true;
    };
    ref.mouseLeaveHandler = function () {
        inside = false;
        instance.invokeMethodAsync('MouseMove', -1, -1);
    };
    ref.clickHandler = function (e) {
      var rect = ref.getBoundingClientRect();
      var x = e.clientX - rect.left;
      var y = e.clientY - rect.top;
      if (!e.target.closest('.rz-marker')) {
        instance.invokeMethodAsync('Click', x, y);
      }
    };

    ref.addEventListener('mouseenter', ref.mouseEnterHandler);
    ref.addEventListener('mouseleave', ref.mouseLeaveHandler);
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
        if (ref != null) {
            ref.innerHTML = value;
        }
    } else {
      return ref.innerHTML;
    }
  },
  execCommand: function (ref, name, value) {
    if (document.activeElement != ref && ref) {
      ref.focus();
    }
    document.execCommand(name, false, value);
    return this.queryCommands(ref);
  },
  queryCommands: function (ref) {
    return {
      html: ref != null ? ref.innerHTML : null,
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
  createEditor: function (ref, uploadUrl, paste, instance, shortcuts) {
    ref.inputListener = function () {
      instance.invokeMethodAsync('OnChange', ref.innerHTML);
    };
    ref.keydownListener = function (e) {
      var key = '';
      if (e.ctrlKey || e.metaKey) {
        key += 'Ctrl+';
      }
      if (e.altKey) {
        key += 'Alt+';
      }
      if (e.shiftKey) {
        key += 'Shift+';
      }
      key += e.code.replace('Key', '').replace('Digit', '').replace('Numpad', '');

      if (shortcuts.includes(key)) {
        e.preventDefault();
        instance.invokeMethodAsync('ExecuteShortcutAsync', key);
      }
    };

    ref.clickListener = function (e) {
      if (e.target) {
        if (e.target.matches('a,button')) {
          e.preventDefault();
        }

        for (var img of ref.querySelectorAll('img.rz-state-selected')) {
          img.classList.remove('rz-state-selected');
        }

        if (e.target.matches('img')) {
          e.target.classList.add('rz-state-selected');
          var range = document.createRange();
          range.selectNode(e.target);
          getSelection().removeAllRanges();
          getSelection().addRange(range);
        }
      }
    }

    ref.selectionChangeListener = function () {
      if (document.activeElement == ref) {
        instance.invokeMethodAsync('OnSelectionChange');
      }
    };
    ref.pasteListener = function (e) {
      var item = e.clipboardData.items[0];

      if (item.kind == 'file') {
        e.preventDefault();
        var file = item.getAsFile();

        if (uploadUrl) {
            var xhr = new XMLHttpRequest();
            var data = new FormData();
            data.append("file", file);
            xhr.onreadystatechange = function (e) {
                if (xhr.readyState === XMLHttpRequest.DONE) {
                    var status = xhr.status;
                    if (status === 0 || (status >= 200 && status < 400)) {
                        var result = JSON.parse(xhr.responseText);
                        var html = '<img src="' + result.url + '">';
                        if (paste) {
                            instance.invokeMethodAsync('OnPaste', html)
                                .then(function (html) {
                                    document.execCommand("insertHTML", false, html);
                                });
                        } else {
                          document.execCommand("insertHTML", false, '<img src="' + result.url + '">');
                        }
                        instance.invokeMethodAsync('OnUploadComplete', xhr.responseText);
                    } else {
                        instance.invokeMethodAsync('OnError', xhr.responseText);
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
        } else {
            var reader = new FileReader();
            reader.onload = function (e) {
              var html = '<img src="' + e.target.result + '">';

              if (paste) {
                instance.invokeMethodAsync('OnPaste', html)
                  .then(function (html) {
                    document.execCommand("insertHTML", false, html);
                  });
              } else {
                document.execCommand("insertHTML", false, html);
              }
            };
            reader.readAsDataURL(file);
        }
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
    ref.addEventListener('keydown', ref.keydownListener);
    ref.addEventListener('click', ref.clickListener);
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
      if(ref) {
          ref.focus();
      }
      var selection = getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
    }
  },
  selectionAttributes: function (selector, attributes, container) {
    var selection = getSelection();
    var range = selection.rangeCount > 0 && selection.getRangeAt(0);
    var parent = range && range.commonAncestorContainer;
    var img = container.querySelector('img.rz-state-selected');
    var inside = img && selector == 'img';
    while (parent) {
      if (parent == container) {
        inside = true;
        break;
      }
      parent = parent.parentNode;
    }
    if (!inside) {
      return {};
    }
    var target = selection.focusNode;
    var innerHTML;

    if (img && selector == 'img') {
      target = img;
    } else if (target) {
      if (target.nodeType == 3) {
        target = target.parentElement;
      } else {
        target = target.childNodes[selection.focusOffset];
        if (target) {
          innerHTML = target.outerHTML;
        }
      }
      if (target && target.matches && !target.matches(selector)) {
        target = target.closest(selector);
      }
    }

    return attributes.reduce(function (result, name) {
      if (target) {
        result[name] = name == 'innerText' ? target[name] : target.getAttribute(name);
      }
      return result;
    }, { innerText: selection.toString(), innerHTML: innerHTML });
  },
  destroyEditor: function (ref) {
    if (ref) {
      ref.removeEventListener('input', ref.inputListener);
      ref.removeEventListener('paste', ref.pasteListener);
      ref.removeEventListener('keydown', ref.keydownListener);
      ref.removeEventListener('click', ref.clickListener);
      document.removeEventListener('selectionchange', ref.selectionChangeListener);
    }
  },
  startDrag: function (ref, instance, handler) {
    if (!ref) {
        return { left: 0, top: 0, width: 0, height: 0 };
    }
    ref.mouseMoveHandler = function (e) {
      instance.invokeMethodAsync(handler, { clientX: e.clientX, clientY: e.clientY });
    };
    ref.touchMoveHandler = function (e) {
      if (e.targetTouches[0] && ref.contains(e.targetTouches[0].target)) {
        instance.invokeMethodAsync(handler, { clientX: e.targetTouches[0].clientX, clientY: e.targetTouches[0].clientY });
      }
    };
    ref.mouseUpHandler = function (e) {
      Radzen.endDrag(ref);
    };
    document.addEventListener('mousemove', ref.mouseMoveHandler);
    document.addEventListener('mouseup', ref.mouseUpHandler);
    document.addEventListener('touchmove', ref.touchMoveHandler, { passive: true, capture: true })
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
      var el = document.getElementById(id + '-drag');
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

      var resizers = cell.parentNode.querySelectorAll('.rz-column-resizer');
      for (let i = 0; i < resizers.length; i++) {
          resizers[i].style.display = 'none';
      }

      Radzen[id + 'end'] = function (e) {
          var el = document.getElementById(id + 'visual');
          if (el) {
              document.body.removeChild(el);
              Radzen[id + 'end'] = null;
              Radzen[id + 'move'] = null;
              var resizers = cell.parentNode.querySelectorAll('.rz-column-resizer');
              for (let i = 0; i < resizers.length; i++) {
                  resizers[i].style.display = 'block';
              }
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
  stopColumnResize: function (id, grid, columnIndex) {
    var el = document.getElementById(id + '-resizer');
    if(!el) return;
    var cell = el.parentNode.parentNode;
    if (!cell) return;
    if (Radzen[el]) {
        grid.invokeMethodAsync(
            'RadzenGrid.OnColumnResized',
            columnIndex,
            cell.getBoundingClientRect().width
        );
        el.style.width = null;
        document.removeEventListener('mousemove', Radzen[el].mouseMoveHandler);
        document.removeEventListener('mouseup', Radzen[el].mouseUpHandler);
        document.removeEventListener('touchmove', Radzen[el].touchMoveHandler)
        document.removeEventListener('touchend', Radzen[el].mouseUpHandler);
        Radzen[el] = null;
    }
  },
  startColumnResize: function(id, grid, columnIndex, clientX) {
      var el = document.getElementById(id + '-resizer');
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
                  el.style.width = null;
                  document.removeEventListener('mousemove', Radzen[el].mouseMoveHandler);
                  document.removeEventListener('mouseup', Radzen[el].mouseUpHandler);
                  document.removeEventListener('touchmove', Radzen[el].touchMoveHandler)
                  document.removeEventListener('touchend', Radzen[el].mouseUpHandler);
                  Radzen[el] = null;
              }
          },
          mouseMoveHandler: function (e) {
              if (Radzen[el]) {
                  var widthFloat = (Radzen[el].width - (Radzen.isRTL(cell) ? -1 : 1) * (Radzen[el].clientX - e.clientX));
                  var minWidth = parseFloat(cell.style.minWidth || 0)

                  if (widthFloat < minWidth) {
                      widthFloat = minWidth;
                  }

                  var width = widthFloat + 'px';

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
      el.style.width = "100%";
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

                    document.removeEventListener('pointerup', Radzen[el].mouseUpHandler);
                    document.removeEventListener('pointermove', Radzen[el].mouseMoveHandler);
                    el.removeEventListener('touchmove', preventDefaultAndStopPropagation);
                    Radzen[el] = null;
                }
            },
            mouseMoveHandler: function(e) {
                if (Radzen[el]) {

                    splitter.invokeMethodAsync(
                        'RadzenSplitter.OnPaneResizing'
                    );

                    var spacePerc = Radzen[el].panePerc + Radzen[el].paneNextPerc;
                    var spaceLength = Radzen[el].paneLength + Radzen[el].paneNextLength;

                    var length = (Radzen[el].paneLength -
                        (isHOrientation && Radzen.isRTL(e.target) ? -1 : 1) * (Radzen[el].clientPos - (isHOrientation ? e.clientX : e.clientY)));

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

        const preventDefaultAndStopPropagation = (ev) => {
            ev.preventDefault();
            ev.stopPropagation();
        };
          document.addEventListener('pointerup', Radzen[el].mouseUpHandler);
          document.addEventListener('pointermove', Radzen[el].mouseMoveHandler);
          el.addEventListener('touchmove', preventDefaultAndStopPropagation, { passive: false });
    },
    resizeSplitter(id, e) {
        var el = document.getElementById(id);
        if (el && Radzen[el]) {
            Radzen[el].mouseMoveHandler(e);
            Radzen[el].mouseUpHandler(e);
        }
    },
    openWaiting: function() {
        if (document.documentElement.scrollHeight > document.documentElement.clientHeight) {
            document.body.classList.add('no-scroll');
        }
        if (Radzen.WaitingIntervalId != null) {
            clearInterval(Radzen.WaitingIntervalId);
        }

        setTimeout(function() {
                var timerObj = document.getElementsByClassName('rz-waiting-timer');
                if (timerObj.length == 0) return;
                var timerStart = new Date().getTime();
                Radzen.WaitingIntervalId = setInterval(function() {
                        if (timerObj == null || timerObj[0] == null) {
                            clearInterval(Radzen.WaitingIntervalId);
                        } else {
                            var time = new Date(new Date().getTime() - timerStart);
                            timerObj[0].innerHTML = Math.floor(time / 1000) + "." + Math.floor((time % 1000) / 100);
                        }
                    },
                    100);
            },
            100);
    },
    closeWaiting: function() {
        document.body.classList.remove('no-scroll');
        if (Radzen.WaitingIntervalId != null) {
            clearInterval(Radzen.WaitingIntervalId);
            Radzen.WaitingIntervalId = null;
        }
    },
    toggleDictation: function (componentRef, language) {
        function start() {
            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

            if (!SpeechRecognition) {
                return;
            }

            radzenRecognition = new SpeechRecognition();
            radzenRecognition.componentRef = componentRef;
            radzenRecognition.continuous = true;

            if (language) {
                radzenRecognition.lang = language;
            }

            radzenRecognition.onresult = function (event) {
                if (event.results.length < 1) {
                    return;
                }

                let current = event.results[event.results.length - 1][0]
                let result = current.transcript;

                componentRef.invokeMethodAsync("OnResult", result);
            };
            radzenRecognition.onend = function (event) {
                componentRef.invokeMethodAsync("StopRecording");
                radzenRecognition = null;
            };
            radzenRecognition.start();
        }

        if (radzenRecognition) {
            if (radzenRecognition.componentRef._id != componentRef._id) {
                radzenRecognition.addEventListener('end', start);
            }
            radzenRecognition.stop();
        } else {
            start();
        }
    }
};
