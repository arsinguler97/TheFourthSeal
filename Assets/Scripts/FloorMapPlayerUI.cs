using System;
using System.Collections;
using UnityEngine;

public class FloorMapPlayerUI : MonoBehaviour
{
    [SerializeField] float moveDuration = 0.75f;

    RectTransform _rectTransform;
    RectTransform _parentRectTransform;
    Canvas _parentCanvas;
    Coroutine _moveRoutine;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _parentRectTransform = _rectTransform != null ? _rectTransform.parent as RectTransform : null;
        _parentCanvas = GetComponentInParent<Canvas>();
    }

    public void SnapTo(RectTransform target)
    {
        if (_rectTransform == null || target == null)
            return;

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _rectTransform.anchoredPosition = GetAnchoredPositionInParentSpace(target);
    }

    public void MoveTo(RectTransform target, Action onComplete)
    {
        if (_rectTransform == null || target == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(MoveToRoutine(target, onComplete));
    }

    IEnumerator MoveToRoutine(RectTransform target, Action onComplete)
    {
        Vector2 startPosition = _rectTransform.anchoredPosition;
        Vector2 targetPosition = GetAnchoredPositionInParentSpace(target);

        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / moveDuration);
            _rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        _rectTransform.anchoredPosition = targetPosition;
        _moveRoutine = null;
        onComplete?.Invoke();
    }

    Vector2 GetAnchoredPositionInParentSpace(RectTransform target)
    {
        if (_rectTransform == null || _parentRectTransform == null || target == null)
            return Vector2.zero;

        Camera eventCamera = _parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _parentCanvas.worldCamera
            : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRectTransform, screenPoint, eventCamera, out Vector2 localPoint);
        return localPoint;
    }
}
