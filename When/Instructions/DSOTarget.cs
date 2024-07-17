using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {
    public class DSOTarget {

        public static InputTarget FindTarget(ISequenceContainer parent) {
            ISequenceContainer sc = ItemUtility.GetRootContainer(parent);
            if (sc != null && sc.Items.Count == 3) {
                return FindRunningItem((ISequenceContainer)sc.Items[1]);
            }
            return null;
        }

        public static InputTarget FindRunningItem(ISequenceContainer c) {
            if (c != null) {
                foreach (var item in c.Items) {
                    if (item is ISequenceContainer sc && (item.Status == SequenceEntityStatus.RUNNING || item.Status == SequenceEntityStatus.CREATED)) {
                        if (item is IDeepSkyObjectContainer dso) {
                            return dso.Target;
                        } else if (item is SequenceContainer cont) {
                            foreach (ISequenceItem item2 in cont.Items) {
                                if (item2.Status == SequenceEntityStatus.RUNNING || item2.Status == SequenceEntityStatus.CREATED) {
                                    if (item2 is IDeepSkyObjectContainer dso2) {
                                        return dso2.Target;
                                    } else if (item2 is ISequenceContainer cont2) {
                                        return FindRunningItem(cont2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
