/**
 * Bar shape with a 4px rounded data-end and a square baseline end, per the mark spec.
 * Recharts' `radius` prop rounds a fixed pair of corners, which is wrong for a diverging
 * bar where the data-end flips to the bottom for negative values.
 */
interface DivergingBarShapeProps {
  x?: number
  y?: number
  width?: number
  height?: number
  fill?: string
  value?: number
}

const RADIUS = 4

export function DivergingBarShape(props: DivergingBarShapeProps) {
  const { x = 0, width = 0, fill, value = 0 } = props
  let { y = 0, height = 0 } = props

  // Recharts gives a negative height for bars below the baseline; normalize to a
  // top-left origin rect first, or the bar gets dropped entirely.
  if (height < 0) {
    y += height
    height = -height
  }
  if (width <= 0 || height <= 0) return null

  const r = Math.min(RADIUS, width / 2, height)
  const negative = value < 0

  // The data-end is the edge away from the zero baseline: bottom for negatives, top otherwise.
  const path = negative
    ? `M${x},${y}
       L${x + width},${y}
       L${x + width},${y + height - r}
       Q${x + width},${y + height} ${x + width - r},${y + height}
       L${x + r},${y + height}
       Q${x},${y + height} ${x},${y + height - r}
       Z`
    : `M${x},${y + r}
       Q${x},${y} ${x + r},${y}
       L${x + width - r},${y}
       Q${x + width},${y} ${x + width},${y + r}
       L${x + width},${y + height}
       L${x},${y + height}
       Z`

  return <path d={path} fill={fill} />
}
