"# ATC_test_v2" 

3/12/2024

Issues:

1) In ListJsonPlaneLocation_zero, planesPreviousLocation is not preserving the values (set in PlaneLocation() method)
This list should be used as a starting position for smoothing transition in TransitionCoroutine.

2) In ButtonPlaneInteraction, if no planes are selected, clicking the button will select corresponding plane. To deselect, the same button should be clicked. However, if another button is clicked, the correct button changes it's color to default. I'm trying to reset its color to RED to hightlight which button is correct (SelectPlane() method)