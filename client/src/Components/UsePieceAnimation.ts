import { useState, useRef, useCallback } from "react";

export interface AnimationState {

  positions: Record<string, number>;
  isAnimating: boolean;
}

const StepDelayMs = 180; 

export function usePieceAnimation(initialPositions: Record<string, number> = {}) {
  const [state, setState] = useState<AnimationState>({
    positions: initialPositions,
    isAnimating: false,
  });
  const animatingRef = useRef(false);

  const animateMove = useCallback(
    (
      playerName: string,
      from: number,
      to: number,
      totalTiles: number,
      onComplete?: () => void
    ): Promise<void> => {
      return new Promise((resolve) => {
        animatingRef.current = true;
        setState((s) => ({ ...s, isAnimating: true }));


        const steps: number[] = [];
        let cur = from;
        while (cur !== to) {
          cur = (cur + 1) % totalTiles;
          steps.push(cur);
        }

        if (steps.length === 0) {
          animatingRef.current = false;
          setState((s) => ({ ...s, isAnimating: false }));
          onComplete?.();
          resolve();
          return;
        }

        let stepIndex = 0;
        const tick = setInterval(() => {
          const tileIdx = steps[stepIndex];
          setState((s) => ({
            ...s,
            positions: { ...s.positions, [playerName]: tileIdx },
          }));
          stepIndex++;
          if (stepIndex >= steps.length) {
            clearInterval(tick);
            animatingRef.current = false;
            setState((s) => ({ ...s, isAnimating: false }));
            onComplete?.();
            resolve();
          }
        }, StepDelayMs);
      });
    },
    []
  );

  const setPosition = useCallback((playerName: string, tileIndex: number) => {
    setState((s) => ({
      ...s,
      positions: { ...s.positions, [playerName]: tileIndex },
    }));
  }, []);

  const setAllPositions = useCallback((positions: Record<string, number>) => {
    setState((s) => ({ ...s, positions }));
  }, []);

  return {
    positions: state.positions,
    isAnimating: state.isAnimating,
    animateMove,
    setPosition,
    setAllPositions,
  };
}