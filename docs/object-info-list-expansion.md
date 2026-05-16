# Object Info list expansion

## Finding

The body/object overview sub-panels such as Facilities and Resources can show
all rows instead of being capped at two rows with their own scrollbar.

The restriction is controlled by the shared game UI base class:

- `Game.UI.Windows.Elements.UIList<T, TData>`
- private fields: `scrollView`, `maxRows`, `itemsInARow`, `rowHeight`,
  `listViewExpanded`
- method: `ConformSizeAndScrollbarsToVisibleContent()`

The object/body overview window calls this sizing method for its sub-lists in
`ObjectInfoWindow.RebuildLayout()`:

- `facilityList`
- `resourcesListExplore`
- `resourcesList`
- `rocketList`
- `launchVehicleList`
- `missionsList`

Useful concrete list types:

- `Game.UI.Windows.Elements.ObjectInfoElements.UIFacilityList`
- `Game.UI.Windows.Elements.ObjectInfoElements.UIResorcesList` (game spelling)
- `Game.UI.Windows.Elements.ObjectInfoElements.UIExploredResourcesList`

## Implementation

UXTweaks adjusts object overview list instances after the game
populates/rebuilds them:

1. Patch `ObjectInfoWindow.RebuildLayout()` with a postfix.
2. For child `UIList<,>` components in that `ObjectInfoWindow`, reflect the
   private base fields.
3. Set `maxRows` to a high value.
4. Set `scrollView.vertical = false` and hide/disable the vertical scrollbar.
5. Set the scroll view `RectTransform` height to `activeRows * rowHeight`.
6. Call `LayoutRebuilder.ForceRebuildLayoutImmediate()` on affected rects.

This keeps the outer `scrollRectAll` as the only scrollbar and removes nested
scrollbar behavior.

## Risk

- Use reflection because the controlling fields are private on the generic
  `UIList<T, TData>` base class.
- The patch is scoped to object overview lists under
  `Game.UI.Windows.Elements.ObjectInfoElements`.
- Very long lists will make object overview content much taller. The outer body
  overview scrollbar remains necessary.
- `UIFacilityList.FrameActive()` and `UIResorcesList.FrameActive()` temporarily
  resize lists during drag/drop; the postfix should re-apply after rebuilds and
  may also need hooks on `FrameActive`/`FrameDeActive` if drag/drop exposes
  regressions.
